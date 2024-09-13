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
using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class MigrationSortantesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public MigrationSortantesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> MigrationSortanteCSV(IFormFile file)
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
				var properties = typeof(MigrationSortante).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
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

					var migrationSortante = new MigrationSortante();

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

							property.SetValue(migrationSortante, convertedValue);
						}
					}

					_context.MigrationSortantes.Add(migrationSortante);
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
					var existingMigrationSortanteId = ex.Entries.First().Entity is MigrationSortante existingMigrationSortante ? existingMigrationSortante.Id : (int?)null;
					return Ok(new { error = $"La migration sortante avec l'id {existingMigrationSortanteId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportMigrationSortanteExcel(IFormFile file)
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

				var properties = typeof(MigrationSortante).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var migrationSortante = new MigrationSortante();

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

							property.SetValue(migrationSortante, convertedValue);
						}
					}

					_context.MigrationSortantes.Add(migrationSortante);
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
					var existingMigrationSortanteId = ex.Entries.First().Entity is MigrationSortante existingMigrationSortante ? existingMigrationSortante.Id : (int?)null;
					return Ok(new { error = $"La migration sortante avec l'id {existingMigrationSortanteId} existe déjà" });
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
		public async Task<ActionResult> ExportMigrationSortanteExcel()
		{
			var migrationSortantes = await _context.MigrationSortantes.ToListAsync();

			var properties = typeof(MigrationSortante).GetProperties()
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
					Name = "Migration_Sortante"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				// Add header row
				var headerRow = new Row();
				foreach (var property in properties)
				{
					headerRow.Append(new Cell() { CellValue = new CellValue(property.Name), DataType = CellValues.String });
				}
				sheetData.AppendChild(headerRow);

				// Add data rows
				foreach (var migrationSortante in migrationSortantes)
				{
					var row = new Row();
					foreach (var property in properties)
					{
						var value = property.GetValue(migrationSortante)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "migration_sortante" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportMigrationSortantes()
		{
			var migrationSortantes = await _context.MigrationSortantes
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(MigrationSortante).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

			foreach (var migrationSortante in migrationSortantes)
			{
				var values = properties.Select(p => p.GetValue(migrationSortante)?.ToString() ?? string.Empty);
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "migration_sortante" + DateTime.Now.ToString() + ".csv");
		}

		[HttpGet("individu/{idMenage}")]
		public async Task<ActionResult<IEnumerable<Individu>>> GetIndividu(int idMenage)
		{
			var individu = await _context.Individus
				.Where(i => i.IdMenage == idMenage && i.Statut == 1)
				.ToListAsync();

			if (individu == null)
			{
				return NotFound();
			}

			return Ok(individu);
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetFilteredMigrationSortantes(FiltreMigrationSortanteDTO filtreMigrationSortanteDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.MigrationSortantes.AsQueryable();

			if (!string.IsNullOrEmpty(filtreMigrationSortanteDto.NumeroMenage))
			{
				query = query.Where(m => m.IdIndividuNavigation.IdMenageNavigation.NumeroMenage.ToLower().Contains(filtreMigrationSortanteDto.NumeroMenage));
			}

			if (filtreMigrationSortanteDto.Statut != -1)
			{
				query = query.Where(m => m.Statut == filtreMigrationSortanteDto.Statut);
			}

			if (filtreMigrationSortanteDto.MotifMigration != -1)
			{
				query = query.Where(m => m.IdMotifMigration == filtreMigrationSortanteDto.MotifMigration);
			}

			if (filtreMigrationSortanteDto.StatutDepart != -1)
			{
				query = query.Where(m => m.StatutDepart == filtreMigrationSortanteDto.StatutDepart);
			}

			if (filtreMigrationSortanteDto.Destination != -1)
			{
				query = query.Where(m => m.Destination == filtreMigrationSortanteDto.Destination);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var migrationSortantes = await query
				.Where(n => n.Statut != -5)
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var migrationSortantesDto = _mapper.Map<IEnumerable<MigrationSortanteDTO>>(migrationSortantes);
			return Ok(new { MigrationSortante = migrationSortantesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetPagedMigrationSortantes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.MigrationSortantes.Where(m => m.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var migrationSortantes = await _context.MigrationSortantes
				.Where(m => m.Statut != -5)
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var migrationSortantesDto = _mapper.Map<IEnumerable<MigrationSortanteDTO>>(migrationSortantes);
			return Ok(new { MigrationSortante = migrationSortantesDto, TotalPages = totalPages });
		}

		// GET: api/MigrationSortantes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetMigrationSortantes()
        {
			var migrationSortantes = await _context.MigrationSortantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.ToListAsync();

			var migrationSortantesDto = _mapper.Map<IEnumerable<MigrationSortanteDTO>>(migrationSortantes);
			return Ok(migrationSortantesDto);
		}

        // GET: api/MigrationSortantes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MigrationSortante>> GetMigrationSortante(int id)
        {
			var migrationSortante = await _context.MigrationSortantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdFokontanyDestinationNavigation)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (migrationSortante == null)
            {
                return NotFound();
            }

			var migrationSortanteDto = _mapper.Map<MigrationSortanteDTO>(migrationSortante);
			return Ok(migrationSortanteDto);
        }

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("valider/{id}")]
		public async Task<IActionResult> ValidateDece(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var migrationSortante = await _context.MigrationSortantes
							.Include(m => m.IdIndividuNavigation)
							.FirstOrDefaultAsync(m => m.Id == id);

					if (migrationSortante == null)
					{
						return NotFound();
					}

					migrationSortante.Statut = 5;
					migrationSortante.IdResponsable = int.Parse(idu);

					if (migrationSortante.Destination == 10 && migrationSortante.StatutDepart == 10)
					{
						migrationSortante.IdIndividuNavigation.Statut = -1;
						_context.Entry(migrationSortante.IdIndividuNavigation).State = EntityState.Modified;
					}

					else if (migrationSortante.NouveauMenage == 10)
					{
						var command = _context.Database.GetDbConnection().CreateCommand();
						command.CommandText = "SELECT nextval('seq_numero_menage')";

						await _context.Database.OpenConnectionAsync();
						var nummen = (long)await command.ExecuteScalarAsync();
						await _context.Database.CloseConnectionAsync();

						var menage = new Menage();
						menage.NumeroMenage = "MENAGE" + nummen.ToString("D6");
						menage.Adresse = migrationSortante.Adresse;
						menage.IdFokontany = (int)migrationSortante.IdFokontanyDestination;

						_context.Menages.Add(menage);
						await _context.SaveChangesAsync();

						migrationSortante.IdIndividuNavigation.IdMenage = menage.Id;
						migrationSortante.IdIndividuNavigation.IsChef = 1;
						_context.Entry(migrationSortante.IdIndividuNavigation).State = EntityState.Modified;
					}

					_context.Entry(migrationSortante).State = EntityState.Modified;

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					if (!MigrationSortanteExists(id))
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
		public async Task<IActionResult> ValidateMigrationSortantes([FromBody] List<int> ids)
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
						var migrationSortante = await _context.MigrationSortantes
							.Include(m => m.IdIndividuNavigation)
							.FirstOrDefaultAsync(m => m.Id == id);

						if (migrationSortante == null)
						{
							return NotFound();
						}

						migrationSortante.Statut = 5;
						migrationSortante.IdResponsable = int.Parse(idu);

						if (migrationSortante.Destination == 10 && migrationSortante.StatutDepart == 10)
						{
							migrationSortante.IdIndividuNavigation.Statut = -1;
							_context.Entry(migrationSortante.IdIndividuNavigation).State = EntityState.Modified;
						} 

						else if (migrationSortante.NouveauMenage == 10)
						{
							var command = _context.Database.GetDbConnection().CreateCommand();
							command.CommandText = "SELECT nextval('seq_numero_menage')";

							await _context.Database.OpenConnectionAsync();
							var nummen = (long)await command.ExecuteScalarAsync();
							await _context.Database.CloseConnectionAsync();

							var menage = new Menage();
							menage.NumeroMenage = "MENAGE" + nummen.ToString("D6");
							menage.Adresse = migrationSortante.Adresse;
							menage.IdFokontany = (int)migrationSortante.IdFokontanyDestination;

							_context.Menages.Add(menage);
							await _context.SaveChangesAsync();

							migrationSortante.IdIndividuNavigation.IdMenage = menage.Id;
							migrationSortante.IdIndividuNavigation.IsChef = 1;
							_context.Entry(migrationSortante.IdIndividuNavigation).State = EntityState.Modified;
						}

						_context.Entry(migrationSortante).State = EntityState.Modified;

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
		public async Task<IActionResult> RejectMigrationSortante(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var migrationSortante = await _context.MigrationSortantes.FindAsync(id);

			if (migrationSortante == null)
			{
				return NotFound();
			}

			migrationSortante.Statut = -5;
			migrationSortante.IdResponsable = int.Parse(idu);

			_context.Entry(migrationSortante).State = EntityState.Modified;

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
				if (!MigrationSortanteExists(id))
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
		public async Task<IActionResult> RejectMigrationSortantes([FromBody] List<int> ids)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			foreach (var id in ids)
			{
				var migrationSortante = await _context.MigrationSortantes.FindAsync(id);

				if (migrationSortante == null)
				{
					return NotFound();
				}

				migrationSortante.Statut = -5;
				migrationSortante.IdResponsable = int.Parse(idu);

				_context.Entry(migrationSortante).State = EntityState.Modified;
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
        public async Task<ActionResult<MigrationSortante>> PostMigrationSortante(MigrationSortanteFormDTO migrationSortanteDto)
        {
			if (migrationSortanteDto.PieceJustificative.Length < 0 || migrationSortanteDto.PieceJustificative == null)
			{
				return Ok(new { error = "La pièce justificative est obligatoire" });
			}

			if (migrationSortanteDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			//var firebaseStorage = await new FirebaseStorage( _configuration["FirebaseStorage:Bucket"])
			//	.Child("migrationSortante")
			//	.Child(migrationSortanteDto.PieceJustificative.FileName)
			//	.PutAsync(migrationSortanteDto.PieceJustificative.OpenReadStream());

			var migrationSortante = _mapper.Map<MigrationSortante>(migrationSortanteDto);

			migrationSortante.PieceJustificative = "-----------";

			_context.MigrationSortantes.Add(migrationSortante);

			//var token = Request.Headers["Authorization"].ToString().Substring(7);
			//var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			//var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			//var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			//var historiqueApplication = new HistoriqueApplication();
			//historiqueApplication.Action = _configuration["Action:Create"];
			//historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			//historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			//historiqueApplication.DateAction = DateTime.Now;
			//historiqueApplication.IdUtilisateur = int.Parse(idu);

			//_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

            return CreatedAtAction("GetMigrationSortante", new { id = migrationSortante.Id }, migrationSortante);
        }

        private bool MigrationSortanteExists(int id)
        {
            return _context.MigrationSortantes.Any(e => e.Id == id);
        }
    }
}
