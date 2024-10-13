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
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Npgsql;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class MenagesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public MenagesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPut("valider")]
		public async Task<ActionResult<string>> ValiderTousNouveauMenage([FromBody] List<int> ids)
		{
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					await _context.Database.OpenConnectionAsync();
                    foreach (int id in ids)
                    {
                    NouveauMenage nouveau = null;

					using (var command = _context.Database.GetDbConnection().CreateCommand())
					{
						// Correction de la commande SQL
						command.CommandText = "SELECT * FROM nouveau_menage WHERE id = @individu";
						command.Parameters.Add(new NpgsqlParameter("@individu", id));

						using (var reader = await command.ExecuteReaderAsync())
						{
							if (await reader.ReadAsync())
							{
								// Remplir l'objet NouveauMenage avec les données du lecteur
								nouveau = new NouveauMenage
								{
									Id = reader.GetInt32(reader.GetOrdinal("id")),
									Menage = reader.GetString(reader.GetOrdinal("menage")),
									Individu = reader.GetString(reader.GetOrdinal("individu"))
								};
							}
						}
					}

					if (nouveau == null)
					{
						return NotFound(new { status = "404", message = "Nouveau menage not found." });
					}

					List<string> infoMenage = nouveau.Menage.Split(',').ToList();
					var menage = new Menage
					{
						NumeroMenage = infoMenage[0],
						Adresse = infoMenage[1],
						IdFokontany = int.Parse(infoMenage[2])
					};

					_context.Menages.Add(menage);
					await _context.SaveChangesAsync();
					int idmenage = menage.Id;

					List<string> individus = nouveau.Individu.Split(';').ToList();
					foreach (var individu in individus)
					{
						List<string> infoIndividu = individu.Split(',').ToList();
						var ind = new Individu
						{
							Nom = infoIndividu[0],
							Prenom = infoIndividu[1],
							DateNaissance = DateOnly.Parse(infoIndividu[2]),
							Sexe = int.Parse(infoIndividu[3]),
							NumActeNaissance = infoIndividu[4],
							Cin = infoIndividu[5],
							Statut = 1,
							IsChef = int.Parse(infoIndividu[6]),
							IdMenage = idmenage
						};

						_context.Individus.Add(ind);
					}
					await _context.SaveChangesAsync();

					using (var commandDelete = _context.Database.GetDbConnection().CreateCommand())
					{
						commandDelete.CommandText = "DELETE FROM nouveau_menage WHERE id = @individu";
						commandDelete.Parameters.Add(new NpgsqlParameter("@individu", id));

						await commandDelete.ExecuteNonQueryAsync();
					}

					await transaction.CommitAsync();
					}

					var token = Request.Headers["Authorization"].ToString().Substring(7);
					var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
					var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
					var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

					var historiqueApplication = new HistoriqueApplication();
					historiqueApplication.Action = _configuration["Action:Validate"];
					historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
					historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
					historiqueApplication.DateAction = DateTime.Now;
					historiqueApplication.IdUtilisateur = int.Parse(idu);

					_context.HistoriqueApplications.Add(historiqueApplication);
					await _context.SaveChangesAsync();

					return Ok(new { status = "200"});
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					await transaction.RollbackAsync();
					return StatusCode(500, new { status = "500", error = ex.Message });
				}
				finally
				{
					await _context.Database.CloseConnectionAsync();
				}
			}
		}

		[HttpPut("refuser")]
		public async Task<ActionResult<string>> RefuserTousNouveauMenage([FromBody] List<int> ids)
		{
            try
			{
				await _context.Database.OpenConnectionAsync();
				foreach (int id in ids)
				{
					using (var commandDelete = _context.Database.GetDbConnection().CreateCommand())
					{
						commandDelete.CommandText = "DELETE FROM nouveau_menage WHERE id = @individu";
						commandDelete.Parameters.Add(new NpgsqlParameter("@individu", id));

						await commandDelete.ExecuteNonQueryAsync();
					}
				}

				var token = Request.Headers["Authorization"].ToString().Substring(7);
				var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
				var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
				var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

				var historiqueApplication = new HistoriqueApplication();
				historiqueApplication.Action = _configuration["Action:Reject"];
				historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
				historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
				historiqueApplication.DateAction = DateTime.Now;
				historiqueApplication.IdUtilisateur = int.Parse(idu);

				_context.HistoriqueApplications.Add(historiqueApplication);
				await _context.SaveChangesAsync();

				return Ok(new { status = "200" });
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return StatusCode(500, new { status = "500", error = ex.Message });
			}
			finally
			{
				await _context.Database.CloseConnectionAsync();
			}
		}

		[HttpPut("valider/{id}")]
		public async Task<ActionResult<string>> ValiderNouveauMenage(int id)
		{
			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
				await _context.Database.OpenConnectionAsync();
				NouveauMenage nouveau = null;

				using (var command = _context.Database.GetDbConnection().CreateCommand())
				{
					// Correction de la commande SQL
					command.CommandText = "SELECT * FROM nouveau_menage WHERE id = @individu";
					command.Parameters.Add(new NpgsqlParameter("@individu", id));

					using (var reader = await command.ExecuteReaderAsync())
					{
						if (await reader.ReadAsync())
						{
							// Remplir l'objet NouveauMenage avec les données du lecteur
							nouveau = new NouveauMenage
							{
								Id = reader.GetInt32(reader.GetOrdinal("id")),
								Menage = reader.GetString(reader.GetOrdinal("menage")),
								Individu = reader.GetString(reader.GetOrdinal("individu"))
							};
						}
					}
				}

				if (nouveau == null)
				{
					return NotFound(new { status = "404", message = "Nouveau menage not found." });
				}

				List<string> infoMenage = nouveau.Menage.Split(',').ToList();
				var menage = new Menage
				{
					NumeroMenage = infoMenage[0],
					Adresse = infoMenage[1],
					IdFokontany = int.Parse(infoMenage[2])
				};

				_context.Menages.Add(menage);
				await _context.SaveChangesAsync();
				int idmenage = menage.Id;

				List<string> individus = nouveau.Individu.Split(';').ToList();
				foreach (var individu in individus)
				{
					List<string> infoIndividu = individu.Split(',').ToList();
					var ind = new Individu
					{
						Nom = infoIndividu[0],
						Prenom = infoIndividu[1],
						DateNaissance = DateOnly.Parse(infoIndividu[2]),
						Sexe = int.Parse(infoIndividu[3]),
						NumActeNaissance = infoIndividu[4],
						Cin = infoIndividu[5],
						Statut = 1,
						IsChef = int.Parse(infoIndividu[6]),
						IdMenage = idmenage
					};

					_context.Individus.Add(ind);
				}
				await _context.SaveChangesAsync();

				using (var commandDelete = _context.Database.GetDbConnection().CreateCommand())
				{
					commandDelete.CommandText = "DELETE FROM nouveau_menage WHERE id = @individu";
					commandDelete.Parameters.Add(new NpgsqlParameter("@individu", id));

					await commandDelete.ExecuteNonQueryAsync();
				}

					await transaction.CommitAsync();

					var token = Request.Headers["Authorization"].ToString().Substring(7);
					var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
					var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
					var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

					var historiqueApplication = new HistoriqueApplication();
					historiqueApplication.Action = _configuration["Action:Validate"];
					historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
					historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
					historiqueApplication.DateAction = DateTime.Now;
					historiqueApplication.IdUtilisateur = int.Parse(idu);

					_context.HistoriqueApplications.Add(historiqueApplication);
					await _context.SaveChangesAsync();

					return Ok(new { status = "200", nouveau });
			}
			catch (Exception ex)
			{
					Console.WriteLine(ex.Message);
					await transaction.RollbackAsync();
					return StatusCode(500, new { status = "500", error = ex.Message });
			}
			finally
			{
				await _context.Database.CloseConnectionAsync();
			}
			}
		}

		[HttpPut("refuser/{id}")]
		public async Task<ActionResult<string>> RefuserNouveauMenage(int id)
		{
				try
				{
					await _context.Database.OpenConnectionAsync();

					using (var commandDelete = _context.Database.GetDbConnection().CreateCommand())
					{
						commandDelete.CommandText = "DELETE FROM nouveau_menage WHERE id = @individu";
						commandDelete.Parameters.Add(new NpgsqlParameter("@individu", id));

						await commandDelete.ExecuteNonQueryAsync();
					}

				var token = Request.Headers["Authorization"].ToString().Substring(7);
				var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
				var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
				var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

				var historiqueApplication = new HistoriqueApplication();
				historiqueApplication.Action = _configuration["Action:Reject"];
				historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
				historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
				historiqueApplication.DateAction = DateTime.Now;
				historiqueApplication.IdUtilisateur = int.Parse(idu);

				_context.HistoriqueApplications.Add(historiqueApplication);
				await _context.SaveChangesAsync();

				return Ok(new { status = "200"});
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					return StatusCode(500, new { status = "500", error = ex.Message });
				}
				finally
				{
					await _context.Database.CloseConnectionAsync();
				}
		}

		[HttpGet("a-valide/{pageNumber}")]
		public async Task<ActionResult> GetPagedMenages(int pageNumber = 1)
		{
			int pageSize = 10;
			var commandCount = _context.Database.GetDbConnection().CreateCommand();
			commandCount.CommandText = "SELECT COUNT(*) FROM nouveau_menage";

			await _context.Database.OpenConnectionAsync();
			var totalItems = (long)await commandCount.ExecuteScalarAsync();

			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var command = _context.Database.GetDbConnection().CreateCommand();
			command.CommandText = $@"
				SELECT id, menage, individu 
				FROM nouveau_menage
				ORDER BY id
				OFFSET {(pageNumber - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

			var menages = new List<NouveauMenage>();

			using (var reader = await command.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
				{
					var menage = new NouveauMenage
					{
						Id = reader.GetInt32(0),
						Menage = reader.GetString(1),
						Individu = reader.GetString(2)
					};

					menages.Add(menage);
				}
			}

			await _context.Database.CloseConnectionAsync();

			return Ok(new { Menages = menages, TotalPages = totalPages });
		}

		[HttpPost("nouveau")]
		public async Task<ActionResult<string>> PostNouveauMenage(NouveauMenageDTO nouveauMenageDto)
		{
			try
			{
				await _context.Database.OpenConnectionAsync();

				using (var command = _context.Database.GetDbConnection().CreateCommand())
				{
					command.CommandText = "INSERT INTO nouveau_menage (menage, individu) VALUES (@menage, @individu)";
					command.Parameters.Add(new NpgsqlParameter("@menage", nouveauMenageDto.menage));
					command.Parameters.Add(new NpgsqlParameter("@individu", nouveauMenageDto.individu));

					await command.ExecuteNonQueryAsync();
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

				return Ok(new { status = "200" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { status = "500", error = ex.Message });
			}
			finally
			{
				await _context.Database.CloseConnectionAsync();
			}
		}

		[HttpGet("numero")]
		public async Task<ActionResult<string>> GetNumeroMenage()
		{
			var command = _context.Database.GetDbConnection().CreateCommand();
			command.CommandText = "SELECT nextval('seq_numero_menage')";

			await _context.Database.OpenConnectionAsync();
			var nummen = (long)await command.ExecuteScalarAsync();
			await _context.Database.CloseConnectionAsync();

			var numero = "MENAGE" + nummen.ToString("D6");
			return Ok(new { Numero = numero });
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> MenageCSV(ImportDTO import)
		{
			if (import.Fichier == null || !import.Fichier.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier CSV" });
			}

			var stream = import.Fichier.OpenReadStream();
			using (var reader = new System.IO.StreamReader(stream))
			{
				string line;
				bool isFirstLine = true;
				var columnMapping = new Dictionary<string, int>();
				var properties = typeof(Menage).GetProperties()
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

					var menage = new Menage();

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

							property.SetValue(menage, convertedValue);
						}
					}

					_context.Menages.Add(menage);
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
					var existingMenageId = ex.Entries.First().Entity is Menage existingMenage ? existingMenage.Id : (int?)null;
					return Ok(new { error = $"Le ménage avec l'id {existingMenageId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportMenageExcel(ImportDTO import)
		{
			if (import.Fichier == null || !import.Fichier.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier XLSX" });
			}

			var stream = import.Fichier.OpenReadStream();
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

				var properties = typeof(Menage).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var menage = new Menage();

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

							property.SetValue(menage, convertedValue);
						}
					}

					_context.Menages.Add(menage);
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
					var existingMenageId = ex.Entries.First().Entity is Menage existingMenage ? existingMenage.Id : (int?)null;
					return Ok(new { error = $"Le ménage avec l'id {existingMenageId} existe déjà" });
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
		public async Task<ActionResult> ExportMenageExcel()
		{
			var menages = await _context.Menages.ToListAsync();

			var properties = typeof(Menage).GetProperties()
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
					Name = "Menage"
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
				foreach (var menage in menages)
				{
					var row = new Row();
					foreach (var property in properties)
					{
						var value = property.GetValue(menage)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "menage" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportMenages()
		{
			var menages = await _context.Menages
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(Menage).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

			foreach (var menage in menages)
			{
				var values = properties.Select(p => p.GetValue(menage)?.ToString() ?? string.Empty);
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "menage" + DateTime.Now.ToString() + ".csv");
		}

		// GET: api/Menages
		[HttpGet]
        public async Task<ActionResult<IEnumerable<MenageDTO>>> GetMenages()
        {
			var menages = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
				    .ThenInclude(f => f.IdCommuneNavigation)
					    .ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.ToListAsync();

			var menagesDto = _mapper.Map<IEnumerable<MenageDTO>>(menages);
			return Ok(menagesDto);
        }

		[HttpGet("numero/{numeroMenage}")]
		public async Task<ActionResult<MenageDTO>> GetMenage(string numeroMenage)
		{
			var menage = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.FirstOrDefaultAsync(m => m.NumeroMenage == numeroMenage);

			if (menage == null)
			{
				return Ok(new { error = "Ménage introuvable"});
			}

			var menageDto = _mapper.Map<MenageDTO>(menage);
			return Ok(menageDto);
		}

		// GET: api/Menages/5
		[HttpGet("{id}")]
        public async Task<ActionResult<MenageDTO>> GetMenage(int id)
        {
			var menage = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.FirstOrDefaultAsync(m => m.Id == id);

            if (menage == null)
            {
				return NotFound();
			}

			var menageDto = _mapper.Map<MenageDTO>(menage);
			return Ok(menageDto);
		}

		// PUT: api/Menages/5
		[HttpPut("{id}")]
        private async Task<IActionResult> PutMenage(int id, Menage menage)
        {
            if (id != menage.Id)
            {
                return BadRequest();
            }

            _context.Entry(menage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MenageExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Menages
        [HttpPost]
        private async Task<ActionResult<Menage>> PostMenage(Menage menage)
        {
            _context.Menages.Add(menage);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMenage", new { id = menage.Id }, menage);
        }

        // DELETE: api/Menages/5
        [HttpDelete("{id}")]
        private async Task<IActionResult> DeleteMenage(int id)
        {
            var menage = await _context.Menages.FindAsync(id);
            if (menage == null)
            {
                return NotFound();
            }

            _context.Menages.Remove(menage);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MenageExists(int id)
        {
            return _context.Menages.Any(e => e.Id == id);
        }
    }
}
