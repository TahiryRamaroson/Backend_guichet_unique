using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Npgsql;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Extensions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Backend_guichet_unique.Services;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class GrossessesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;

		public GrossessesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration, EmailService emailService)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
			_emailService = emailService;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> GrossesseCSV(IFormFile file)
		{
			if (file == null || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier CSV" });
			}

			var stream = file.OpenReadStream();
			using (var reader = new System.IO.StreamReader(stream))
			{
				string line;
				bool isFirstLine = true;
				var columnMapping = new Dictionary<string, int>();
				var properties = typeof(Grossesse).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.Where(p => p.Name != "IdAntecedentMedicauxes") // Ignore the IdAntecedentMedicauxes property
					.ToList();

				while ((line = reader.ReadLine()) != null)
				{
					var values = line.Split(',');

					if (isFirstLine)
					{
						for (int i = 0; i < values.Length; i++)
						{
							columnMapping[values[i]] = i;
						}
						isFirstLine = false;
						continue;
					}

					var grossesse = new Grossesse();
					var antecedentIds = new List<int>();

					foreach (var property in properties)
					{
						if (columnMapping.TryGetValue(property.Name, out int columnIndex))
						{
							var cellValue = values[columnIndex];
							object convertedValue = null;

							if (property.PropertyType == typeof(DateOnly))
							{
								convertedValue = DateOnly.Parse(cellValue);
							}
							else if (property.PropertyType == typeof(int))
							{
								convertedValue = int.Parse(cellValue);
							}
							else if (Nullable.GetUnderlyingType(property.PropertyType) != null)
							{
								var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
								convertedValue = string.IsNullOrEmpty(cellValue) ? null : Convert.ChangeType(cellValue, underlyingType);
							}
							else
							{
								convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
							}

							property.SetValue(grossesse, convertedValue);
						}
					}

					// Get the IdAntecedentMedicaux column value
					if (columnMapping.TryGetValue("IdAntecedentMedicaux", out int antecedentColumnIndex))
					{
						var antecedentCellValue = values[antecedentColumnIndex];
						antecedentIds = antecedentCellValue.Split('/').Select(int.Parse).ToList();
					}

					_context.Grossesses.Add(grossesse);
					await _context.SaveChangesAsync();

					// Insert antecedent medical records
					foreach (int idAnt in antecedentIds)
					{
						var command = _context.Database.GetDbConnection().CreateCommand();
						command.CommandText = @"
                    INSERT INTO grossesse_antecedant_medicaux (id_grossesse, id_antecedent_medicaux)
                    VALUES (@grossesseId, @antecedentId)";

						command.Parameters.Add(new NpgsqlParameter("@grossesseId", grossesse.Id));
						command.Parameters.Add(new NpgsqlParameter("@antecedentId", idAnt));

						await _context.Database.OpenConnectionAsync();
						await command.ExecuteNonQueryAsync();
						await _context.Database.CloseConnectionAsync();
					}
				}

				var token = Request.Headers["Authorization"].ToString().Substring(7);
				var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
				var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
				var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

				var historiqueApplication = new HistoriqueApplication();
				historiqueApplication.Action = _configuration["Action:Import"];
				historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
				historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
				historiqueApplication.DateAction = DateTime.Now;
				historiqueApplication.IdUtilisateur = int.Parse(idu);

				_context.HistoriqueApplications.Add(historiqueApplication);

				try
				{
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateException ex)
				{
					var existingGrossesseId = ex.Entries.First().Entity is Grossesse existingGrossesse ? existingGrossesse.Id : (int?)null;
					return Ok(new { error = $"La grossesse avec l'id {existingGrossesseId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportGrossesseExcel(IFormFile file)
		{
			if (file == null || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier XLSX" });
			}

			var stream = file.OpenReadStream();
			using (var document = SpreadsheetDocument.Open(stream, false))
			{
				var workbookPart = document.WorkbookPart;
				var worksheetPart = workbookPart.WorksheetParts.First();
				var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
				var rows = sheetData.Elements<Row>().ToList();

				// Get the header row
				var headerRow = rows.First();
				var headerCells = headerRow.Elements<Cell>().ToList();
				var columnMapping = new Dictionary<string, int>();

				for (int i = 0; i < headerCells.Count; i++)
				{
					var cellValue = GetCellValue(document, headerCells[i]);
					columnMapping[cellValue] = i;
				}

				var properties = typeof(Grossesse).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.Where(p => p.Name != "IdAntecedentMedicauxes") // Ignore the IdAntecedentMedicauxes property
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var grossesse = new Grossesse();
					var antecedentIds = new List<int>();

					foreach (var property in properties)
					{
						if (columnMapping.TryGetValue(property.Name, out int columnIndex))
						{
							var cellValue = GetCellValue(document, cells[columnIndex]);
							object convertedValue = null;

							if (property.PropertyType == typeof(DateOnly))
							{
								convertedValue = DateOnly.Parse(cellValue);
							}
							else if (property.PropertyType == typeof(int))
							{
								convertedValue = int.Parse(cellValue);
							}
							else if (Nullable.GetUnderlyingType(property.PropertyType) != null)
							{
								var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
								convertedValue = string.IsNullOrEmpty(cellValue) ? null : Convert.ChangeType(cellValue, underlyingType);
							}
							else
							{
								convertedValue = Convert.ChangeType(cellValue, property.PropertyType);
							}

							property.SetValue(grossesse, convertedValue);
						}
					}

					// Get the IdAntecedentMedicaux column value
					if (columnMapping.TryGetValue("IdAntecedentMedicaux", out int antecedentColumnIndex))
					{
						var antecedentCellValue = GetCellValue(document, cells[antecedentColumnIndex]);
						antecedentIds = antecedentCellValue.Split('/').Select(int.Parse).ToList();
					}

					_context.Grossesses.Add(grossesse);
					await _context.SaveChangesAsync();

					// Insert antecedent medical records
					foreach (int idAnt in antecedentIds)
					{
						var command = _context.Database.GetDbConnection().CreateCommand();
						command.CommandText = @"
                    INSERT INTO grossesse_antecedant_medicaux (id_grossesse, id_antecedent_medicaux)
                    VALUES (@grossesseId, @antecedentId)";

						command.Parameters.Add(new NpgsqlParameter("@grossesseId", grossesse.Id));
						command.Parameters.Add(new NpgsqlParameter("@antecedentId", idAnt));

						await _context.Database.OpenConnectionAsync();
						await command.ExecuteNonQueryAsync();
						await _context.Database.CloseConnectionAsync();
					}
				}

				var token = Request.Headers["Authorization"].ToString().Substring(7);
				var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
				var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
				var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

				var historiqueApplication = new HistoriqueApplication();
				historiqueApplication.Action = _configuration["Action:Import"];
				historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
				historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
				historiqueApplication.DateAction = DateTime.Now;
				historiqueApplication.IdUtilisateur = int.Parse(idu);

				_context.HistoriqueApplications.Add(historiqueApplication);

				try
				{
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateException ex)
				{
					var existingGrossesseId = ex.Entries.First().Entity is Grossesse existingGrossesse ? existingGrossesse.Id : (int?)null;
					return Ok(new { error = $"La grossesse avec l'id {existingGrossesseId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		private string GetCellValue(SpreadsheetDocument document, Cell cell)
		{
			var value = cell.CellValue?.Text;
			if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
			{
				return document.WorkbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(value)).InnerText;
			}
			return value;
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportGrossessesExcel()
		{
			var grossesses = await _context.Grossesses
				.Include(g => g.IdAntecedentMedicauxes) // Assurez-vous de charger les antécédents médicaux
				.ToListAsync();

			var properties = typeof(Grossesse).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			var stream = new System.IO.MemoryStream();
			using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
			{
				var workbookPart = document.AddWorkbookPart();
				workbookPart.Workbook = new Workbook();
				var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
				worksheetPart.Worksheet = new Worksheet(new SheetData());

				var sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
				var sheet = new Sheet()
				{
					Id = document.WorkbookPart.GetIdOfPart(worksheetPart),
					SheetId = 1,
					Name = "Grossesse"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				// Add header row
				var headerRow = new Row();
				foreach (var property in properties.Where(p => p.Name != "IdAntecedentMedicauxes"))
				{
					headerRow.Append(new Cell() { CellValue = new CellValue(property.Name), DataType = CellValues.String });
				}
				headerRow.Append(new Cell() { CellValue = new CellValue("IdAntecedentMedicaux"), DataType = CellValues.String });
				sheetData.AppendChild(headerRow);

				// Add data rows
				foreach (var grossesse in grossesses)
				{
					var row = new Row();
					foreach (var property in properties.Where(p => p.Name != "IdAntecedentMedicauxes"))
					{
						var value = property.GetValue(grossesse)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					row.Append(new Cell() { CellValue = new CellValue(GetAntecedentIds(grossesse)), DataType = CellValues.String });
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "grossesse" + DateTime.Now.ToString() + ".xlsx"
			};

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Export"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return content;
		}

		[HttpGet("export/csv")]
		public async Task<IActionResult> ExportGrossesses()
		{
			var grossesses = await _context.Grossesses
				.Include(g => g.IdAntecedentMedicauxes)
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(Grossesse).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties
				.Where(p => p.Name != "IdAntecedentMedicauxes")
				.Select(p => p.Name)
				.Concat(new[] { "IdAntecedentMedicaux" })));

			foreach (var grossesse in grossesses)
			{
				var values = properties
					.Where(p => p.Name != "IdAntecedentMedicauxes")
					.Select(p => p.GetValue(grossesse)?.ToString() ?? string.Empty)
					.ToList();
				values.Add(GetAntecedentIds(grossesse));
				builder.AppendLine(string.Join(",", values));
			}

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Export"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "grossesse" + DateTime.Now.ToString() + ".csv");
		}

		private string GetAntecedentIds(Grossesse grossesse)
		{
			return string.Join("/", grossesse.IdAntecedentMedicauxes.Select(a => a.Id));
		}

		[HttpGet("mere/{idMenage}")]
		public async Task<ActionResult<IEnumerable<Individu>>> GetMere(int idMenage)
		{
			var mere = await _context.Individus
				.Where(i => i.IdMenage == idMenage && i.Sexe == 0)
				.GroupJoin(_context.Grossesses,
					individu => individu.Id,
					grossesse => grossesse.IdMere,
					(individu, grossesses) => new { Individu = individu, Grossesses = grossesses })
				.SelectMany(
					x => x.Grossesses.DefaultIfEmpty(),
					(x, grossesse) => new { Individu = x.Individu, Grossesse = grossesse })
				.Where(joined => joined.Grossesse == null || (joined.Grossesse.StatutGrossesse == 5 && joined.Grossesse.Statut == 5))
				.Select(joined => joined.Individu)
				.ToListAsync();

			if (mere == null || !mere.Any())
			{
				return Ok(new { error = "Toutes les femmes de ce ménage sont déjà enregistrées comme étant enceintes" });
			}

			return Ok(mere);
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<GrossesseDTO>>> GetFilteredGrossesses(FiltreGrossesseDTO filtreGrossesseDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.Grossesses.AsQueryable();

			if (filtreGrossesseDto.Dpa.HasValue)
			{
				query = query.Where(g => g.DateAccouchement.Equals(filtreGrossesseDto.Dpa));
			}

			if (!string.IsNullOrEmpty(filtreGrossesseDto.NumeroMenage))
			{
				query = query.Where(g => g.IdMereNavigation.IdMenageNavigation.NumeroMenage.ToLower().Contains(filtreGrossesseDto.NumeroMenage));
			}

			if (filtreGrossesseDto.Statut != -1)
			{
				query = query.Where(g => g.Statut == filtreGrossesseDto.Statut);
			}

			if (filtreGrossesseDto.RisqueComplication != -1)
			{
				var complications = _configuration.GetSection("Grossesse").Get<GrossesseSettings>().Complication;
				var complication = complications.FirstOrDefault(c => c.Id == filtreGrossesseDto.RisqueComplication);
				query = query.Where(g => (g.RisqueComplication > complication.Min) && (g.RisqueComplication < complication.Max) );
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var grossesses = await query
				.Where(n => n.Statut != -5)
				.Include(grossesse => grossesse.IdMereNavigation)
				.Include(grossesse => grossesse.IdIntervenantNavigation)
				.Include(grossesse => grossesse.IdResponsableNavigation)
				.Include(grossesse => grossesse.IdAntecedentMedicauxes)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var grossessesDto = _mapper.Map<IEnumerable<GrossesseDTO>>(grossesses);
			return Ok(new { Grossesse = grossessesDto, TotalPages = totalPages });
		}

		[HttpGet("complications")]
		public ActionResult<IEnumerable<string>> GetComplications()
		{
			var complications = _configuration.GetSection("Grossesse").Get<GrossesseSettings>().Complication;
			return Ok(complications);
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<GrossesseDTO>>> GetPagedGrossesses(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Grossesses.Where(g => g.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var grossesses = await _context.Grossesses
				.Where(grossesse => grossesse.Statut != -5)
				.Include(grossesse => grossesse.IdMereNavigation)
				.Include(grossesse => grossesse.IdIntervenantNavigation)
				.Include(grossesse => grossesse.IdResponsableNavigation)
				.Include(grossesse => grossesse.IdAntecedentMedicauxes)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var grossessesDto = _mapper.Map<IEnumerable<GrossesseDTO>>(grossesses);
			return Ok(new { Grossesse = grossessesDto, TotalPages = totalPages });
		}

		[HttpGet]
        public async Task<ActionResult<IEnumerable<GrossesseDTO>>> GetGrossesses()
        {
            var grossesses = await _context.Grossesses
                .Include(grossesse => grossesse.IdMereNavigation)
                .Include(grossesse => grossesse.IdIntervenantNavigation)
				.Include(grossesse => grossesse.IdResponsableNavigation)
				.Include(grossesse => grossesse.IdAntecedentMedicauxes)
				.ToListAsync();

			var grossessesDto = _mapper.Map<IEnumerable<GrossesseDTO>>(grossesses);

			return Ok(grossessesDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Grossesse>> GetGrossesse(int id)
        {
			var grossesse = await _context.Grossesses
				.Include(grossesse => grossesse.IdMereNavigation)
				.Include(grossesse => grossesse.IdIntervenantNavigation)
				.Include(grossesse => grossesse.IdResponsableNavigation)
				.Include(grossesse => grossesse.IdAntecedentMedicauxes)
				.FirstOrDefaultAsync(grossesse => grossesse.Id == id);

			if (grossesse == null)
            {
                return NotFound();
            }

            return Ok(grossesse);
        }

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("valider/{id}")]
		public async Task<IActionResult> ValidateGrossesse(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var grossesse = await _context.Grossesses.FindAsync(id);

					if (grossesse == null)
					{
						return NotFound();
					}

					grossesse.Statut = 5;
					grossesse.IdResponsable = int.Parse(idu);

					_context.Entry(grossesse).State = EntityState.Modified;

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					if (!GrossesseExists(id))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Validate"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);
			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("valider")]
		public async Task<IActionResult> ValidateGrossesses([FromBody] List<int> ids)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					foreach (var id in ids)
					{
						var grossesse = await _context.Grossesses.FindAsync(id);

						if (grossesse == null)
						{
							return NotFound();
						}

						grossesse.Statut = 5;
						grossesse.IdResponsable = int.Parse(idu);

						_context.Entry(grossesse).State = EntityState.Modified;

					}
					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					return NotFound();
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Validate"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);
			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("refuser/{id}")]
		public async Task<IActionResult> RejectGrossesse(int id)
		{

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var grossesse = await _context.Grossesses.FindAsync(id);
			grossesse.Statut = -5;
			grossesse.IdResponsable = int.Parse(idu);

			_context.Entry(grossesse).State = EntityState.Modified;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Reject"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!GrossesseExists(id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return Ok(new { status = "200" });
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("refuser")]
		public async Task<IActionResult> RejectGrossesses([FromBody] List<int> ids)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			foreach (var id in ids)
			{
				var grossesse = await _context.Grossesses.FindAsync(id);
				grossesse.Statut = -5;
				grossesse.IdResponsable = int.Parse(idu);

				_context.Entry(grossesse).State = EntityState.Modified;
			}

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Reject"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				return NotFound();
			}

			return Ok(new { status = "200" });
		}

		[Authorize(Policy = "IntervenantPolicy")]
		[HttpPost]
        public async Task<ActionResult<Grossesse>> PostGrossesse(GrossesseFormDTO grossesseDto)
        {
			if (grossesseDto.PieceJustificative.Length < 0  || grossesseDto.PieceJustificative == null)
			{
				return Ok(new { error = "La pièce justificative est obligatoire" });
			}

			if(grossesseDto.DerniereRegle > DateOnly.FromDateTime(DateTime.Now))
			{
				return Ok(new { error = "La date de la dernière règle doit être inférieure à la date du jour" });
			}

			if (grossesseDto.DerniereRegle >= grossesseDto.DateAccouchement)
			{
				return Ok(new { error = "La date de la dernière règle doit être inférieure à la date prévue de l'accouchement" });
			}

			if (grossesseDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			//var firebaseStorage = await new FirebaseStorage("guichet-unique-upload.appspot.com")
			//	.Child("grossesse")
			//	.Child(grossesseDto.PieceJustificative.FileName)
			//	.PutAsync(grossesseDto.PieceJustificative.OpenReadStream());

			var grossesse = _mapper.Map<Grossesse>(grossesseDto);

			grossesse.PieceJustificative = "-----------";

			_context.Grossesses.Add(grossesse);
            await _context.SaveChangesAsync();

			foreach (int idAnt in grossesseDto.AntecedentMedicaux)
            {
				var command = _context.Database.GetDbConnection().CreateCommand();
				command.CommandText = @"
                    INSERT INTO grossesse_antecedant_medicaux (id_grossesse, id_antecedent_medicaux)
                    VALUES (@grossesseId, @antecedentId)";

				command.Parameters.Add(new NpgsqlParameter("@grossesseId", grossesse.Id));
				command.Parameters.Add(new NpgsqlParameter("@antecedentId", idAnt));

				await _context.Database.OpenConnectionAsync();
				await command.ExecuteNonQueryAsync();
				await _context.Database.CloseConnectionAsync();
			}

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Create"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return CreatedAtAction("GetGrossesse", new { id = grossesse.Id }, grossesse);
        }

        private bool GrossesseExists(int id)
        {
            return _context.Grossesses.Any(e => e.Id == id);
        }
    }
}
