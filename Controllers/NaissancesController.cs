﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using AutoMapper;
using Firebase.Storage;
using Backend_guichet_unique.Services;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsablePolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class NaissancesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly EmailService _emailService;
		private readonly IConfiguration _configuration;

		public NaissancesController(GuichetUniqueContext context, IMapper mapper, EmailService emailService, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_emailService = emailService;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> NaissancesCSV(IFormFile file)
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
				var properties = typeof(Naissance).GetProperties()
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

					var naissance = new Naissance();

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

							property.SetValue(naissance, convertedValue);
						}
					}

					_context.Naissances.Add(naissance);
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
					var existingNaissanceId = ex.Entries.First().Entity is Naissance existingNaissance ? existingNaissance.Id : (int?)null;
					return Ok(new { error = $"La naissance avec l'id {existingNaissanceId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportNaissancesExcel(IFormFile file)
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

				var properties = typeof(Naissance).GetProperties()
					.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
					.ToList();

				foreach (var row in rows.Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var naissance = new Naissance();

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

							property.SetValue(naissance, convertedValue);
						}
					}

					_context.Naissances.Add(naissance);
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
					var existingNaissanceId = ex.Entries.First().Entity is Naissance existingNaissance ? existingNaissance.Id : (int?)null;
					return Ok(new { error = $"La naissance avec l'id {existingNaissanceId} existe déjà" });
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
		public async Task<ActionResult> ExportNaissancesExcel()
		{
			var naissances = await _context.Naissances.ToListAsync();

			var properties = typeof(Naissance).GetProperties()
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
					Name = "Naissance"
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
				foreach (var naissance in naissances)
				{
					var row = new Row();
					foreach (var property in properties)
					{
						var value = property.GetValue(naissance)?.ToString() ?? string.Empty;
						row.Append(new Cell() { CellValue = new CellValue(value), DataType = CellValues.String });
					}
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "naissances" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportNaissances()
		{
			var naissances = await _context.Naissances
				.ToListAsync();

			var builder = new System.Text.StringBuilder();

			var properties = typeof(Naissance).GetProperties()
				.Where(p => !p.PropertyType.IsClass || p.PropertyType == typeof(string))
				.ToList();

			builder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

			foreach (var naissance in naissances)
			{
				var values = properties.Select(p => p.GetValue(naissance)?.ToString() ?? string.Empty);
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "naissance" + DateTime.Now.ToString() + ".csv");
		}

		[HttpGet("mere/{idMenage}")]
		public async Task<ActionResult<IEnumerable<Individu>>> GetMere(int idMenage)
		{
			var mere = await _context.Individus
				.Where(i => (i.IdMenage == idMenage && i.Sexe == 0))
				.ToListAsync();

			if (mere == null)
			{
				return NotFound();
			}

			return mere;
		}

		[HttpGet("pere/{idMenage}")]
		public async Task<ActionResult<IEnumerable<Individu>>> GetPere(int idMenage)
		{
			var pere = await _context.Individus
				.Where(i => (i.IdMenage == idMenage && i.Sexe == 1))
                .ToListAsync();

			if (pere == null)
			{
				return NotFound();
			}

			return pere;
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<NaissanceDTO>>> GetFilteredNaissances(FiltreNaissanceDTO filtreNaissanceDto, int pageNumber = 1)
		{
			int pageSize = 10;

			var query = _context.Naissances.AsQueryable();

			if (!string.IsNullOrEmpty(filtreNaissanceDto.NomNouveauNe))
			{
				query = query.Where(n => n.NomNouveauNe.ToLower().Contains(filtreNaissanceDto.NomNouveauNe) ||
										 n.PrenomNouveauNe.ToLower().Contains(filtreNaissanceDto.NomNouveauNe));
			}

			if (!string.IsNullOrEmpty(filtreNaissanceDto.NumeroMenage))
			{
				query = query.Where(n => n.IdMenageNavigation.NumeroMenage.ToLower().Contains(filtreNaissanceDto.NumeroMenage));
			}

			if (filtreNaissanceDto.Statut != -1)
			{
				query = query.Where(n => n.Statut == filtreNaissanceDto.Statut);
			}

			if (filtreNaissanceDto.Sexe != -1)
			{
				query = query.Where(n => n.Sexe == filtreNaissanceDto.Sexe);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var naissances = await query
				.Where(n => n.Statut != -5)
				.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(n => n.IdMenageNavigation)
				.Include(n => n.IdPereNavigation)
				.Include(n => n.IdMereNavigation)
				.Include(n => n.IdIntervenantNavigation)
				.Include(n => n.IdResponsableNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var naissancesDto = _mapper.Map<IEnumerable<NaissanceDTO>>(naissances);
			return Ok(new { Naissances = naissancesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<NaissanceDTO>>> GetPagedNaissances(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Naissances.Where(n => n.Statut != -5).CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var naissances = await _context.Naissances
				.Where(n => n.Statut != -5)
				.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(n => n.IdMenageNavigation)
				.Include(n => n.IdPereNavigation)
				.Include(n => n.IdMereNavigation)
				.Include(n => n.IdIntervenantNavigation)
				.Include(n => n.IdResponsableNavigation)
				.OrderByDescending(n => n.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var naissancesDto = _mapper.Map<IEnumerable<NaissanceDTO>>(naissances);
			return Ok(new { Naissances = naissancesDto, TotalPages = totalPages });
		}

		// GET: api/Naissances
		[HttpGet]
        public async Task<ActionResult<IEnumerable<NaissanceDTO>>> GetNaissances()
        {
			var naissances = await _context.Naissances
				.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(n => n.IdMenageNavigation)
				.Include(n => n.IdPereNavigation)
				.Include(n => n.IdMereNavigation)
				.Include(n => n.IdIntervenantNavigation)
				.Include(n => n.IdResponsableNavigation)
				.ToListAsync();

			var naissancesDto = _mapper.Map<IEnumerable<NaissanceDTO>>(naissances);
			return Ok(naissancesDto);
		}

        // GET: api/Naissances/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NaissanceDTO>> GetNaissance(int id)
        {
			var naissance = await _context.Naissances
				.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(n => n.IdMenageNavigation)
				.Include(n => n.IdPereNavigation)
				.Include(n => n.IdMereNavigation)
				.Include(n => n.IdIntervenantNavigation)
				.Include(n => n.IdResponsableNavigation)
				.FirstOrDefaultAsync(n => n.Id == id);


			if (naissance == null)
            {
                return NotFound();
            }

			var naissanceDto = _mapper.Map<NaissanceDTO>(naissance);

			return Ok(naissanceDto);
        }

		[Authorize(Policy = "ResponsablePolicy")]
		[HttpPut("valider/{id}")]
		public async Task<IActionResult> ValidateNaissance(int id)
		{
			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var naissance = await _context.Naissances.FindAsync(id);
					if (naissance == null)
					{
						return NotFound();
					}

					naissance.Statut = 5;
					naissance.IdResponsable = int.Parse(idu);
					_context.Entry(naissance).State = EntityState.Modified;

					var individu = new Individu
					{
						Nom = naissance.NomNouveauNe,
						Prenom = naissance.PrenomNouveauNe,
						Sexe = naissance.Sexe,
						DateNaissance = naissance.DateNaissance,
						IdMenage = naissance.IdMenage,
						NumActeNaissance = naissance.NumActeNaissance,
						IsChef = -1
					};
					_context.Individus.Add(individu);

					await _context.SaveChangesAsync();

					if (naissance.IdPere != null)
					{
						var lienPere = new LienParente
						{
							IdEnfant = individu.Id,
							IdParent = (int)naissance.IdPere,
							IdTypeLien = 1
						};
						_context.LienParentes.Add(lienPere);
					}

					if (naissance.IdMere != null)
					{
						var lienMere = new LienParente
						{
							IdEnfant = individu.Id,
							IdParent = (int)naissance.IdMere,
							IdTypeLien = 2
						};
						_context.LienParentes.Add(lienMere);
					}

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					await transaction.RollbackAsync();
					if (!NaissanceExists(id))
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
		public async Task<IActionResult> ValidateNaissances([FromBody] List<int> ids)
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
						var naissance = await _context.Naissances.FindAsync(id);
						if (naissance == null)
						{
							return NotFound();
						}

						naissance.Statut = 5;
						naissance.IdResponsable = int.Parse(idu);
						_context.Entry(naissance).State = EntityState.Modified;

						var individu = new Individu
						{
							Nom = naissance.NomNouveauNe,
							Prenom = naissance.PrenomNouveauNe,
							Sexe = naissance.Sexe,
							DateNaissance = naissance.DateNaissance,
							IdMenage = naissance.IdMenage,
							NumActeNaissance = naissance.NumActeNaissance,
							IsChef = -1
						};
						_context.Individus.Add(individu);

						await _context.SaveChangesAsync();

						if (naissance.IdPere != null)
						{
							var lienPere = new LienParente
							{
								IdEnfant = individu.Id,
								IdParent = (int)naissance.IdPere,
								IdTypeLien = 1
							};
							_context.LienParentes.Add(lienPere);
						}

						if (naissance.IdMere != null)
						{
							var lienMere = new LienParente
							{
								IdEnfant = individu.Id,
								IdParent = (int)naissance.IdMere,
								IdTypeLien = 2
							};
							_context.LienParentes.Add(lienMere);
						}
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
		public async Task<IActionResult> RejectNaissance(int id)
		{
			var naissance = await _context.Naissances.FindAsync(id);
			naissance.Statut = -5;

			_context.Entry(naissance).State = EntityState.Modified;

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

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!NaissanceExists(id))
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
		public async Task<IActionResult> RejectNaissances([FromBody] List<int> ids)
		{
			foreach (var id in ids)
			{
				var naissance = await _context.Naissances.FindAsync(id);
				naissance.Statut = -5;

				_context.Entry(naissance).State = EntityState.Modified;
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
        public async Task<ActionResult<Naissance>> PostNaissance(NaissanceFormDTO naissanceDto)
        {
			if (naissanceDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			//var firebaseStorage = await new FirebaseStorage("guichet-unique-upload.appspot.com")
			//	.Child("naissance")
			//	.Child(naissanceDto.PieceJustificative.FileName)
			//	.PutAsync(naissanceDto.PieceJustificative.OpenReadStream());

			var naissance = _mapper.Map<Naissance>(naissanceDto);

            //naissance.PieceJustificative = firebaseStorage;
            naissance.PieceJustificative = "-----------";

			_context.Naissances.Add(naissance);

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

            //string toEmail = "taxramaroson2@gmail.com";
			//string subject = "Nouvelle naissance";
			//string body = "Une nouvelle naissance a été enregistrée.";
			//await _emailService.SendEmailAsync(toEmail, subject, body);


			return CreatedAtAction("GetNaissance", new { id = naissance.Id }, naissance);
        }

        private bool NaissanceExists(int id)
        {
            return _context.Naissances.Any(e => e.Id == id);
        }
    }
}
