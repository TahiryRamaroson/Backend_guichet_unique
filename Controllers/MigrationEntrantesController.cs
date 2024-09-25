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
using NuGet.Packaging;
using Firebase.Storage;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class MigrationEntrantesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public MigrationEntrantesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> MigrationEntranteCSV(IFormFile file)
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
				var properties = typeof(MigrationEntrante).GetProperties()
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

					var migrationEntrante = new MigrationEntrante();

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
							else if (property.PropertyType == typeof(DateOnly?))
							{
								convertedValue = string.IsNullOrEmpty(cellValue) ? (DateOnly?)null : DateOnly.Parse(cellValue);
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


							property.SetValue(migrationEntrante, convertedValue);
						}
					}

					_context.MigrationEntrantes.Add(migrationEntrante);
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
					var existingMigrationEntranteId = ex.Entries.First().Entity is MigrationEntrante existingMigrationEntrante ? existingMigrationEntrante.Id : (int?)null;
					return Ok(new { error = $"La migration entrante avec l'id {existingMigrationEntranteId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportMigrationEntranteExcel(IFormFile file)
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

				var properties = typeof(MigrationEntrante).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var migrationEntrante = new MigrationEntrante();

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
							else if (property.PropertyType == typeof(DateOnly?))
							{
								convertedValue = string.IsNullOrEmpty(cellValue) ? (DateOnly?)null : DateOnly.Parse(cellValue);
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


							property.SetValue(migrationEntrante, convertedValue);
						}
					}

					_context.MigrationEntrantes.Add(migrationEntrante);
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
					var existingMigrationEntranteId = ex.Entries.First().Entity is MigrationEntrante existingMigrationEntrante ? existingMigrationEntrante.Id : (int?)null;
					return Ok(new { error = $"La migration entrante avec l'id {existingMigrationEntranteId} existe déjà" });
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
		public async Task<ActionResult> ExportMigrationEntranteExcel()
		{
			var migrationEntrantes = await _context.MigrationEntrantes.ToListAsync();

			var properties = typeof(MigrationEntrante).GetProperties()
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
					Name = "Migration_Entrante"
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
				foreach (var migrationEntrante in migrationEntrantes)
				{
					var row = new Row();
					foreach (var property in properties)
					{
						var value = property.GetValue(migrationEntrante)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "migration_entrante" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportMigrationEntrantes()
		{
			var migrationEntrantes = await _context.MigrationEntrantes
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(MigrationEntrante).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

			foreach (var migrationEntrante in migrationEntrantes)
			{
				var values = properties.Select(p => p.GetValue(migrationEntrante)?.ToString() ?? string.Empty);
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "migration_entrante" + DateTime.Now.ToString() + ".csv");
		}

		[HttpGet("individu/{idMenage}")]
		public async Task<ActionResult<IEnumerable<MigrationInfoIndividuDTO>>> GetIndividu(int idMenage)
		{
			var menage = await _context.Menages.FindAsync(idMenage);

			var migrationSortantes = await _context.MigrationSortantes
				.Where(m => m.IdFokontanyDestination == menage.IdFokontany && m.Statut == 5 && m.NouveauMenage == -10)
				.ToListAsync();

			var info = new List<MigrationInfoIndividuDTO>();
			foreach (var migrationSortante in migrationSortantes)
			{
				MigrationInfoIndividuDTO migrationInfoIndividu = new MigrationInfoIndividuDTO();
				var individu = await _context.Individus
					.Where(i => i.Id == migrationSortante.IdIndividu && i.Statut == 1)
					.Include(i => i.IdMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
							.ThenInclude(f => f.IdCommuneNavigation)
								.ThenInclude(c => c.IdDistrictNavigation)
									.ThenInclude(d => d.IdRegionNavigation)
					.ToListAsync();

                foreach (var item in individu)
                {
					var individuDtos = _mapper.Map<IndividuDTO>(item);
					migrationInfoIndividu.Individu = individuDtos;
					migrationInfoIndividu.MigrationSortante = migrationSortante;
				}

                info.Add(migrationInfoIndividu);
			}

			//if (individus == null)
			//{
			//	return Ok(new { error = "Aucun individu enregistré"});
			//}

			return Ok(info);
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MigrationEntranteDTO>>> GetFilteredMigrationEntrantes(FiltreMigrationEntranteDTO filtreMigrationEntranteDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.MigrationEntrantes.AsQueryable();

			if (!string.IsNullOrEmpty(filtreMigrationEntranteDto.NumeroMenage))
			{
				query = query.Where(m => m.IdAncienMenageNavigation.NumeroMenage.ToLower().Contains(filtreMigrationEntranteDto.NumeroMenage) ||
										m.IdNouveauMenageNavigation.NumeroMenage.ToLower().Contains(filtreMigrationEntranteDto.NumeroMenage)
									);
			}

			if (filtreMigrationEntranteDto.Statut != -1)
			{
				query = query.Where(m => m.Statut == filtreMigrationEntranteDto.Statut);
			}

			if (filtreMigrationEntranteDto.MotifMigration != -1)
			{
				query = query.Where(m => m.IdMotifMigration == filtreMigrationEntranteDto.MotifMigration);
			}

			if (filtreMigrationEntranteDto.StatutResidence != -1)
			{
				query = query.Where(m => m.StatutResidence == filtreMigrationEntranteDto.StatutResidence);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var migrationEntrantes = await query
				.Where(n => n.Statut != -5)
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdAncienMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.Include(m => m.IdNouveauMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var migrationEntrantesDto = _mapper.Map<IEnumerable<MigrationEntranteDTO>>(migrationEntrantes);
			return Ok(new { MigrationEntrante = migrationEntrantesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MigrationSortanteDTO>>> GetPagedMigrationEntrantes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.MigrationEntrantes.Where(m => m.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var migrationEntrantes = await _context.MigrationEntrantes
				.Where(m => m.Statut != -5)
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdAncienMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.Include(m => m.IdNouveauMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var migrationEntrantesDto = _mapper.Map<IEnumerable<MigrationEntranteDTO>>(migrationEntrantes);
			return Ok(new { MigrationEntrante = migrationEntrantesDto, TotalPages = totalPages });
		}

		// GET: api/MigrationEntrantes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<MigrationEntranteDTO>>> GetMigrationEntrantes()
        {
			var migrationEntrantes = await _context.MigrationEntrantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdAncienMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.Include(m => m.IdNouveauMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.ToListAsync();

			var migrationEntrantesDto = _mapper.Map<IEnumerable<MigrationEntranteDTO>>(migrationEntrantes);
			return Ok(migrationEntrantesDto);
		}

        // GET: api/MigrationEntrantes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MigrationEntranteDTO>> GetMigrationEntrante(int id)
        {
			var migrationEntrante = await _context.MigrationEntrantes
				.Include(m => m.IdMotifMigrationNavigation)
				.Include(m => m.IdIndividuNavigation)
				.Include(m => m.IdIntervenantNavigation)
				.Include(m => m.IdResponsableNavigation)
				.Include(m => m.IdAncienMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.Include(m => m.IdNouveauMenageNavigation)
					.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(m => m.IdCommuneNavigation)
							.ThenInclude(m => m.IdDistrictNavigation)
								.ThenInclude(m => m.IdRegionNavigation)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (migrationEntrante == null)
            {
                return NotFound();
            }

			var migrationEntranteDto = _mapper.Map<MigrationEntranteDTO>(migrationEntrante);
			return Ok(migrationEntranteDto);
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
					var migrationEntrante = await _context.MigrationEntrantes
							.Include(m => m.IdIndividuNavigation)
							.FirstOrDefaultAsync(m => m.Id == id);

					if (migrationEntrante == null)
					{
						return NotFound();
					}

					migrationEntrante.Statut = 5;
					migrationEntrante.IdResponsable = int.Parse(idu);

					if (migrationEntrante.StatutResidence == 10)
					{
						migrationEntrante.IdIndividuNavigation.IdMenage = migrationEntrante.IdNouveauMenage;
						_context.Entry(migrationEntrante.IdIndividuNavigation).State = EntityState.Modified;
					}

					_context.Entry(migrationEntrante).State = EntityState.Modified;

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					if (!MigrationEntranteExists(id))
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
		public async Task<IActionResult> ValidateMigrationEntrantes([FromBody] List<int> ids)
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
						var migrationEntrante = await _context.MigrationEntrantes
							.Include(m => m.IdIndividuNavigation)
							.FirstOrDefaultAsync(m => m.Id == id);

						if (migrationEntrante == null)
						{
							return NotFound();
						}

						migrationEntrante.Statut = 5;
						migrationEntrante.IdResponsable = int.Parse(idu);

						if (migrationEntrante.StatutResidence == 10)
						{
							migrationEntrante.IdIndividuNavigation.IdMenage = migrationEntrante.IdNouveauMenage;
							_context.Entry(migrationEntrante.IdIndividuNavigation).State = EntityState.Modified;
						}

						_context.Entry(migrationEntrante).State = EntityState.Modified;

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
		public async Task<IActionResult> RejectMigrationEntrante(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var migrationEntrante = await _context.MigrationEntrantes.FindAsync(id);

			if (migrationEntrante == null)
			{
				return NotFound();
			}

			migrationEntrante.Statut = -5;
			migrationEntrante.IdResponsable = int.Parse(idu);

			_context.Entry(migrationEntrante).State = EntityState.Modified;

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
				if (!MigrationEntranteExists(id))
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
		public async Task<IActionResult> RejectMigrationEntrantes([FromBody] List<int> ids)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			foreach (var id in ids)
			{
				var migrationEntrante = await _context.MigrationEntrantes.FindAsync(id);

				if (migrationEntrante == null)
				{
					return NotFound();
				}

				migrationEntrante.Statut = -5;
				migrationEntrante.IdResponsable = int.Parse(idu);

				_context.Entry(migrationEntrante).State = EntityState.Modified;
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
        public async Task<ActionResult<MigrationEntrante>> PostMigrationEntrante(MigrationEntranteFormDTO migrationEntranteDto)
        {
			if (migrationEntranteDto.PieceJustificative.Length < 0 || migrationEntranteDto.PieceJustificative == null)
			{
				return Ok(new { error = "La pièce justificative est obligatoire" });
			}

			if (migrationEntranteDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			var firebaseStorage = await new FirebaseStorage(_configuration["FirebaseStorage:Bucket"])
				.Child("migrationEntrante")
				.Child(migrationEntranteDto.PieceJustificative.FileName)
				.PutAsync(migrationEntranteDto.PieceJustificative.OpenReadStream());

			var migrationEntrante = _mapper.Map<MigrationEntrante>(migrationEntranteDto);

			migrationEntrante.PieceJustificative = firebaseStorage;

			_context.MigrationEntrantes.Add(migrationEntrante);

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

            return CreatedAtAction("GetMigrationEntrante", new { id = migrationEntrante.Id }, migrationEntrante);
        }

        private bool MigrationEntranteExists(int id)
        {
            return _context.MigrationEntrantes.Any(e => e.Id == id);
        }
    }
}
