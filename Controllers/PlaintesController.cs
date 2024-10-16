using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Trainers;
using System.Numerics;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using Google;
using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Npgsql;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class PlaintesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		public IConfiguration _configuration;
		private readonly IWebHostEnvironment _environment;

		public PlaintesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration, IWebHostEnvironment environment)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
			_environment = environment;
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpGet("nettoyer")]
		public async Task<ActionResult> DeleteFiles()
		{
			string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

			if (Directory.Exists(uploadFolder))
			{
				DirectoryInfo di = new DirectoryInfo(uploadFolder);

				foreach (FileInfo file in di.GetFiles())
				{
					file.Delete();
				}
			}

			return Ok(new { status = "200" });
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPost("entrainement")]
		public async Task<ActionResult> EntrainerModelCsv()
		{
			// Charger le contexte de machine learning
			var mlContext = new MLContext();

			// Charger les données d'entraînement
			var data = mlContext.Data.LoadFromTextFile<PlainteModel>("Data/*", hasHeader: true, separatorChar: ',');

			// Créer un pipeline de transformation de données et d'entraînement
			var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", new TextFeaturizingEstimator.Options
			{
				OutputTokensColumnName = "Tokens",
				CaseMode = TextNormalizingEstimator.CaseMode.Lower,
				KeepDiacritics = false,
				KeepPunctuations = false,
				StopWordsRemoverOptions = new StopWordsRemovingEstimator.Options
				{
					Language = TextFeaturizingEstimator.Language.French
				},
				WordFeatureExtractor = new WordBagEstimator.Options
				{
					NgramLength = 1,
					UseAllLengths = true,
					Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf
				}
			}, nameof(PlainteModel.Description))
			.Append(mlContext.Transforms.Conversion.MapValueToKey("Label"))
			.Append(mlContext.Transforms.Concatenate("Features", "Features"))
			.Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
			.Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

			// Entraîner le modèle
			ITransformer model = pipeline.Fit(data);

			// Enregistrer le modèle
			using (var fileStream = new FileStream("model.zip", FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				mlContext.Model.Save(model, data.Schema, fileStream);
			}

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Train"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

		[HttpPost("entrainementDatabase")]
		public async Task<ActionResult> EntrainerModelDatabase()
		{
			// Charger le contexte de machine learning
			var mlContext = new MLContext();

			var plaintes = await _context.Plaintes
					.Select(p => new PlainteModel
					{
						Description = p.Description,
						Label = (uint)p.IdCategoriePlainte
					})
					.ToListAsync();

			// afficher dans la console les plaintes
			foreach (var plainte in plaintes)
			{
				Console.WriteLine($"Description : {plainte.Description} - Label : {plainte.Label}");
			}

			var data = mlContext.Data.LoadFromEnumerable(plaintes);

				// Créer un pipeline de transformation de données et d'entraînement
				var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", new TextFeaturizingEstimator.Options
				{
					OutputTokensColumnName = "Tokens",
					CaseMode = TextNormalizingEstimator.CaseMode.Lower,
					KeepDiacritics = false,
					KeepPunctuations = false,
					StopWordsRemoverOptions = new StopWordsRemovingEstimator.Options
					{
						Language = TextFeaturizingEstimator.Language.French
					},
					WordFeatureExtractor = new WordBagEstimator.Options
					{
						NgramLength = 1,
						UseAllLengths = true,
						Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf
					}
				}, nameof(PlainteModel.Description))
				.Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(PlainteModel.Label)))
				.Append(mlContext.Transforms.Concatenate("Features", "Features"))
				.Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
				.Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

			// Entraîner le modèle
			ITransformer model = pipeline.Fit(data);

			// Enregistrer le modèle
			using (var fileStream = new FileStream("model.zip", FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				mlContext.Model.Save(model, data.Schema, fileStream);
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("prediction")]
		public async Task<ActionResult<CategoriePlainte>> PredictCategory([FromBody] string desc)
		{
			try
			{
				// Charger le contexte de machine learning
				var mlContext = new MLContext();

				// Charger le modèle entraîné (à faire une seule fois et enregistrer le modèle)
				ITransformer model = mlContext.Model.Load("model.zip", out var modelInputSchema);

				// Créer un moteur de prédiction
				var predictionEngine = mlContext.Model.CreatePredictionEngine<PlainteModel, PlaintePrediction>(model);

				// Créer une instance de PlainteModel pour la prédiction
				var plainte = new PlainteModel { Description = desc };

				Console.WriteLine($"Description : ----------------------------- {plainte.Description} -----------------------------");

				// Prédire la catégorie de la plainte
				var prediction = predictionEngine.Predict(plainte);

				Console.WriteLine($"Prédiction : ----------------------------- {prediction.PredictedLabel} -----------------------------");

				// Vérifier la prédiction
				if (prediction == null)
				{
					return BadRequest("La prédiction a échoué.");
				}

				var categoriePlainte = await _context.CategoriePlaintes.FindAsync((int)prediction.PredictedLabel);

				if (categoriePlainte == null)
				{
					return NotFound();
				}

				return Ok(categoriePlainte);
			}
			catch (Exception ex)
			{
				// Enregistrer l'erreur
				Console.WriteLine($"Erreur lors de la prédiction : {ex.Message}");
				return StatusCode(500, "Erreur interne du serveur.");
			}
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPost("import/modele")]
		public async Task<IActionResult> UploadFile(ImportDTO import)
		{
			if (import.Fichier == null || !import.Fichier.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier CSV" });
			}

			string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

			if (!Directory.Exists(uploadFolder))
			{
				Directory.CreateDirectory(uploadFolder);
			}

			var path = Path.Combine(uploadFolder, import.Fichier.FileName);

			using (var reader = new StreamReader(import.Fichier.OpenReadStream()))
			{
				var headerLine = await reader.ReadLineAsync();
				var headers = headerLine.Split(',');

				if (headers.Length != 2 || headers[0] != "Description" || headers[1] != "Label")
				{
					return Ok(new { error = "Structure du fichier CSV incorrecte" });
				}
			}

			using (var stream = new FileStream(path, FileMode.Create))
			{
				await import.Fichier.CopyToAsync(stream);
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

			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> PlainteCSV(ImportDTO import)
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
				var properties = typeof(Plainte).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.Where(p => p.Name != "HistoriqueActionPlaintes")
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

					var plainte = new Plainte();

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

							property.SetValue(plainte, convertedValue);
						}
					}

					_context.Plaintes.Add(plainte);
					await _context.SaveChangesAsync();
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
					var existingPlainteId = ex.Entries.First().Entity is Plainte existingPlainte ? existingPlainte.Id : (int?)null;
					return Ok(new { error = $"La plainte avec l'id {existingPlainteId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportPlainteExcel(ImportDTO import)
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

				var properties = typeof(Plainte).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.Where(p => p.Name != "HistoriqueActionPlaintes")
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var plainte = new Plainte();

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

							property.SetValue(plainte, convertedValue);
						}
					}

					_context.Plaintes.Add(plainte);
					await _context.SaveChangesAsync();
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
					var existingPlainteId = ex.Entries.First().Entity is Plainte existingPlainte ? existingPlainte.Id : (int?)null;
					return Ok(new { error = $"La plainte avec l'id {existingPlainteId} existe déjà" });
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

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpGet("export/modele")]
		public async Task<IActionResult> ExportModeleCsv()
		{
			var plaintes = await _context.Plaintes.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Description,Label");

			foreach (var plainte in plaintes)
			{
				builder.AppendLine($"\"{plainte.Description}\",{plainte.IdCategoriePlainte}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "modele_" + DateTime.Now.ToString("fffffff") + ".csv");
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportPlainteExcel()
		{
			var plaintes = await _context.Plaintes.ToListAsync();

			var properties = typeof(Plainte).GetProperties()
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
					Name = "Plainte"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				// Add header row
				var headerRow = new Row();
				foreach (var property in properties.Where(p => p.Name != "HistoriqueActionPlaintes"))
				{
					headerRow.Append(new Cell() { CellValue = new CellValue(property.Name), DataType = CellValues.String });
				}
				sheetData.AppendChild(headerRow);

				// Add data rows
				foreach (var plainte in plaintes)
				{
					var row = new Row();
					foreach (var property in properties.Where(p => p.Name != "HistoriqueActionPlaintes"))
					{
						var value = property.GetValue(plainte)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "plainte" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportPlaintes()
		{
			var plaintes = await _context.Plaintes
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(Plainte).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties
				.Where(p => p.Name != "HistoriqueActionPlaintes")
				.Select(p => p.Name)));

			foreach (var plainte in plaintes)
			{
				var values = properties
					.Where(p => p.Name != "HistoriqueActionPlaintes")
					.Select(p => p.GetValue(plainte)?.ToString() ?? string.Empty)
					.ToList();
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "plainte" + DateTime.Now.ToString() + ".csv");
		}

		[HttpGet("victime/{idMenage}")]
		public async Task<ActionResult<IEnumerable<Individu>>> GetVictime(int idMenage)
		{
			var victime = await _context.Individus
				.Where(i => i.IdMenage == idMenage && i.Statut == 1)
				.ToListAsync();

			if (victime == null)
			{
				return NotFound();
			}

			return Ok(victime);
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPost("filtre/valide/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<PlainteDTO>>> GetFilteredPlaintes(FiltrePlainteValideDTO filtrePlainteValideDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.Plaintes.AsQueryable();

			if (!string.IsNullOrEmpty(filtrePlainteValideDto.NumeroMenage))
			{
				query = query.Where(p => p.IdVictimeNavigation.IdMenageNavigation.NumeroMenage.ToLower().Contains(filtrePlainteValideDto.NumeroMenage));
			}

			if (filtrePlainteValideDto.StatutTraitement != -1)
			{
				query = query.Where(p => p.StatutTraitement == filtrePlainteValideDto.StatutTraitement);
			}

			if (filtrePlainteValideDto.idCategoriePlainte != -1)
			{
				query = query.Where(p => p.IdCategoriePlainte == filtrePlainteValideDto.idCategoriePlainte);
			}

			if (filtrePlainteValideDto.DateFait.HasValue)
			{
				query = query.Where(p => p.DateFait.Equals(filtrePlainteValideDto.DateFait));
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var plaintes = await query
				.Where(p => p.Statut == 5)
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(v => v.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
					.ThenInclude(h => h.IdActions)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var plaintesDto = _mapper.Map<IEnumerable<PlainteDTO>>(plaintes);
			return Ok(new { Plainte = plaintesDto, TotalPages = totalPages });
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<PlainteDTO>>> GetFilteredPlaintes(FiltrePlainteDTO filtrePlainteDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.Plaintes.AsQueryable();

			if (!string.IsNullOrEmpty(filtrePlainteDto.NumeroMenage))
			{
				query = query.Where(p => p.IdVictimeNavigation.IdMenageNavigation.NumeroMenage.ToLower().Contains(filtrePlainteDto.NumeroMenage.ToLower()));
			}

			if (filtrePlainteDto.Statut != -1)
			{
				query = query.Where(p => p.Statut == filtrePlainteDto.Statut);
			}

			if (filtrePlainteDto.idCategoriePlainte != -1)
			{
				query = query.Where(p => p.IdCategoriePlainte == filtrePlainteDto.idCategoriePlainte);
			}

			if (filtrePlainteDto.DateFait.HasValue)
			{
				query = query.Where(p => p.DateFait.Equals(filtrePlainteDto.DateFait));
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var plaintes = await query
				.Where(p => p.Statut != -5)
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var plaintesDto = _mapper.Map<IEnumerable<PlainteDTO>>(plaintes);
			return Ok(new { Plainte = plaintesDto, TotalPages = totalPages });
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpGet("valide/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<PlainteDTO>>> GetPagedPlaintesValide(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Plaintes.Where(m => m.Statut == 5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var plaintes = await _context.Plaintes
				.Where(p => p.Statut == 5)
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(v => v.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
					.ThenInclude(h => h.IdActions)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var plaintesDto = _mapper.Map<IEnumerable<PlainteDTO>>(plaintes);
			return Ok(new { Plainte = plaintesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<PlainteDTO>>> GetPagedPlaintes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Plaintes.Where(m => m.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var plaintes = await _context.Plaintes
				.Where(p => p.Statut != -5)
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(m => m.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var plaintesDto = _mapper.Map<IEnumerable<PlainteDTO>>(plaintes);
			return Ok(new { Plainte = plaintesDto, TotalPages = totalPages });
		}

		[HttpGet]
        public async Task<ActionResult<IEnumerable<PlainteDTO>>> GetPlaintes()
        {
			var plaintes = await _context.Plaintes
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(v => v.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
				    .ThenInclude(h => h.IdActions)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.ToListAsync();

			var plaintesDto = _mapper.Map<IEnumerable<PlainteDTO>>(plaintes);

			return Ok(plaintesDto);
		}

        [HttpGet("{id}")]
        public async Task<ActionResult<PlainteDTO>> GetPlainte(int id)
        {
			var plainte = await _context.Plaintes
				.Include(p => p.IdVictimeNavigation)
					.ThenInclude(v => v.IdMenageNavigation)
				.Include(p => p.IdIntervenantNavigation)
				.Include(p => p.IdResponsableNavigation)
				.Include(p => p.HistoriqueActionPlaintes)
					.ThenInclude(h => h.IdActions)
				.Include(p => p.IdCategoriePlainteNavigation)
				.Include(p => p.IdFokontanyFaitNavigation)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (plainte == null)
			{
				return NotFound();
			}

			var plainteDto = _mapper.Map<PlainteDTO>(plainte);
			return Ok(plainteDto);
		}

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("valider/{id}")]
		public async Task<IActionResult> ValidatePlainte(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var plainte = await _context.Plaintes.FindAsync(id);

					if (plainte == null)
					{
						return NotFound();
					}

					plainte.Statut = 5;
					plainte.IdResponsable = int.Parse(idu);

					_context.Entry(plainte).State = EntityState.Modified;

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					if (!PlainteExists(id))
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
		public async Task<IActionResult> ValidatePlaintes([FromBody] List<int> ids)
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
						var plainte = await _context.Plaintes.FindAsync(id);

						if (plainte == null)
						{
							return NotFound();
						}

						plainte.Statut = 5;
						plainte.IdResponsable = int.Parse(idu);

						_context.Entry(plainte).State = EntityState.Modified;

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
		public async Task<IActionResult> RejectPlainte(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var plainte = await _context.Plaintes.FindAsync(id);

			if (plainte == null)
			{
				return NotFound();
			}

			plainte.Statut = -5;
			plainte.IdResponsable = int.Parse(idu);

			_context.Entry(plainte).State = EntityState.Modified;

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
				if (!PlainteExists(id))
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
		public async Task<IActionResult> RejectPlaintes([FromBody] List<int> ids)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			foreach (var id in ids)
			{
				var plainte = await _context.Plaintes.FindAsync(id);

				if (plainte == null)
				{
					return NotFound();
				}

				plainte.Statut = -5;
				plainte.IdResponsable = int.Parse(idu);

				_context.Entry(plainte).State = EntityState.Modified;
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

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("cloturer/{id}")]
		public async Task<IActionResult> ClosePlaintes(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

				var plainte = await _context.Plaintes.FindAsync(id);

				if (plainte == null)
				{
					return NotFound();
				}

				plainte.StatutTraitement = 10;
				plainte.IdResponsable = int.Parse(idu);

				_context.Entry(plainte).State = EntityState.Modified;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Close"];
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
        public async Task<ActionResult<Plainte>> PostPlainte(PlainteFormDTO plainteDto)
        {
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var ResultatPrediction = 0;
			try
			{
				var mlContext = new MLContext();

				ITransformer model = mlContext.Model.Load("model.zip", out var modelInputSchema);

				var predictionEngine = mlContext.Model.CreatePredictionEngine<PlainteModel, PlaintePrediction>(model);

				var plainteModel = new PlainteModel { Description = plainteDto.Description };

				var prediction = predictionEngine.Predict(plainteModel);
				ResultatPrediction = (int)prediction.PredictedLabel;

				if (prediction == null)
				{
					return BadRequest("La prédiction a échoué.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erreur lors de la prédiction : {ex.Message}");
				return StatusCode(500, "Erreur interne du serveur.");
			}

			var plainte = new Plainte();
			plainte.IdVictime = plainteDto.Victime;
			plainte.IdIntervenant = int.Parse(idu);
			plainte.Description = plainteDto.Description;
			plainte.DateFait = DateOnly.FromDateTime(DateTime.Now);
			plainte.Statut = 0;
			plainte.StatutTraitement = 0;
			plainte.IdFokontanyFait = plainteDto.FokontanyFait;
			plainte.IdCategoriePlainte = ResultatPrediction;

			_context.Plaintes.Add(plainte);

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Create"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlainte", new { id = plainte.Id }, plainte);
        }

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPost("Actions/{idplainte}")]
		public async Task<ActionResult<IEnumerable<PlainteDTO>>> PostActionPlainte(PlainteActionFormDTO plainteActionDto, int idplainte)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var historiqueActionPlainte = new HistoriqueActionPlainte();
					historiqueActionPlainte.IdPlainte = idplainte;
					historiqueActionPlainte.IdResponsable = int.Parse(idu);
					historiqueActionPlainte.DateVisite = plainteActionDto.DateVisite;
					_context.HistoriqueActionPlaintes.Add(historiqueActionPlainte);

					await _context.SaveChangesAsync();

					var plainte = await _context.Plaintes.FindAsync(idplainte);
					plainte.StatutTraitement = 5;
					_context.Entry(plainte).State = EntityState.Modified;

					foreach (int idAction in plainteActionDto.Actions)
                    {
						var command = _context.Database.GetDbConnection().CreateCommand();
						command.CommandText = @"
                    INSERT INTO action_plainte (id_historique_action_plainte, id_action)
                    VALUES (@historiqueActionPlainteId, @actionId)";

						command.Parameters.Add(new NpgsqlParameter("@historiqueActionPlainteId", historiqueActionPlainte.Id));
						command.Parameters.Add(new NpgsqlParameter("@actionId", idAction));

						await _context.Database.OpenConnectionAsync();
						await command.ExecuteNonQueryAsync();
						await _context.Database.CloseConnectionAsync();
					}

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
			historiqueApplication.Action = _configuration["Action:Create"];
			historiqueApplication.Composant = "ActionPlainte";
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

		private bool PlainteExists(int id)
        {
            return _context.Plaintes.Any(e => e.Id == id);
        }
    }
}
