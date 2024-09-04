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
using System.Security.Cryptography.Pkcs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "AdministrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class HistoriqueApplicationsController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public HistoriqueApplicationsController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("export/excel")]
		public async Task<ActionResult> ExportHistoriqueApplicationsExcel(FiltreHistoriqueApplicationExportDTO filtreExport)
		{
			var historiqueApplications = await _context.HistoriqueApplications
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.Where(h => DateOnly.FromDateTime(h.DateAction ?? DateTime.Now) >= filtreExport.debut && DateOnly.FromDateTime(h.DateAction ?? DateTime.Now) <= filtreExport.fin)
				.ToListAsync();

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
					Name = "Historique_Application du " + filtreExport.debut.ToString() + " au " + filtreExport.fin.ToString()
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
					new Cell() { CellValue = new CellValue("Utilisateur"), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue("Profil"), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue("Composant"), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue("Action"), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue("Date"), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue("Url"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var historiqueApplication in historiqueApplications)
				{
					var row = new Row();
					row.Append(
						new Cell() { CellValue = new CellValue(historiqueApplication.IdUtilisateurNavigation.Nom + " " + historiqueApplication.IdUtilisateurNavigation.Prenom), DataType = CellValues.String },
						new Cell() { CellValue = new CellValue(historiqueApplication.IdUtilisateurNavigation.IdProfilNavigation.Nom), DataType = CellValues.String },
						new Cell() { CellValue = new CellValue(historiqueApplication.Composant), DataType = CellValues.String },
						new Cell() { CellValue = new CellValue(historiqueApplication.Action), DataType = CellValues.String },
						new Cell() { CellValue = new CellValue(historiqueApplication.DateAction.ToString()), DataType = CellValues.String },
						new Cell() { CellValue = new CellValue(historiqueApplication.UrlAction), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "historiqueApplication" + DateTime.Now.ToString() + ".xlsx"
			};

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var newhistoriqueApplication = new HistoriqueApplication();
			newhistoriqueApplication.Action = _configuration["Action:Export"];
			newhistoriqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			newhistoriqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			newhistoriqueApplication.DateAction = DateTime.Now;
			newhistoriqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(newhistoriqueApplication);

			await _context.SaveChangesAsync();

			return content;
		}

		[HttpPost("export/csv")]
		public async Task<IActionResult> ExportHistoriqueApplications(FiltreHistoriqueApplicationExportDTO filtreExport)
		{
			var historiqueApplications = await _context.HistoriqueApplications
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.Where(h => DateOnly.FromDateTime(h.DateAction ?? DateTime.Now) >= filtreExport.debut && DateOnly.FromDateTime(h.DateAction ?? DateTime.Now) <= filtreExport.fin)
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Utilisateur,Profil,Composant,Action,Date,url");

			foreach (var historiqueApplication in historiqueApplications)
			{
				builder.AppendLine($"{historiqueApplication.IdUtilisateurNavigation.Nom + " " + historiqueApplication.IdUtilisateurNavigation.Prenom},{historiqueApplication.IdUtilisateurNavigation.IdProfilNavigation.Nom},{historiqueApplication.Composant},{historiqueApplication.Action},{historiqueApplication.DateAction},{historiqueApplication.UrlAction}");
			}

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var newhistoriqueApplication = new HistoriqueApplication();
			newhistoriqueApplication.Action = _configuration["Action:Export"];
			newhistoriqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			newhistoriqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			newhistoriqueApplication.DateAction = DateTime.Now;
			newhistoriqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(newhistoriqueApplication);

			await _context.SaveChangesAsync();

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "historiqueApplication" + DateTime.Now.ToString() + ".csv");
		}

		// fonction pour récupérer les actions dans le fichier de configuration
		[HttpGet("action")]
		public ActionResult<IEnumerable<string>> GetActions()
		{
			var actions = new List<string>();
			var actionsConfig = _configuration.GetSection("Action").GetChildren();
			foreach (var action in actionsConfig)
			{
				actions.Add(action.Value);
			}
			return Ok(actions);
		}

		[HttpGet("composant")]
		public ActionResult<IEnumerable<string>> GetComposants()
		{
			var controllers = new List<string>();
			var controllerTypes = typeof(Program).Assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type));
			foreach (var controllerType in controllerTypes)
			{
				controllers.Add(controllerType.Name.Replace("Controller", ""));
			}
			return Ok(controllers);
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<HistoriqueApplicationDTO>>> GetFilteredFokontanies(FiltreHistoriqueApplicationDTO filtreHistoriqueApplicationDto, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreHistoriqueApplicationDto.text?.ToLower() ?? string.Empty;

			var query = _context.HistoriqueApplications.AsQueryable();

			if (!string.IsNullOrEmpty(text))
			{
				query = query.Where(h => h.IdUtilisateurNavigation.Nom.ToLower().Contains(text) ||
										 h.IdUtilisateurNavigation.Prenom.ToLower().Contains(text) ||
										 h.IdUtilisateurNavigation.IdProfilNavigation.Nom.ToLower().Contains(text));
			}

			if (!string.IsNullOrEmpty(filtreHistoriqueApplicationDto.composant))
			{
				query = query.Where(h => h.Composant == filtreHistoriqueApplicationDto.composant);
			}

			if (!string.IsNullOrEmpty(filtreHistoriqueApplicationDto.action))
			{
				query = query.Where(h => h.Action == filtreHistoriqueApplicationDto.action);
			}

			if (filtreHistoriqueApplicationDto.date.HasValue)
			{
				var date = filtreHistoriqueApplicationDto.date;
				query = query.Where(h => DateOnly.FromDateTime(h.DateAction ?? DateTime.Now) == date);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var historiqueApplications = await query
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var historiqueApplicationsDto = _mapper.Map<IEnumerable<HistoriqueApplicationDTO>>(historiqueApplications);

			return Ok(new { HistoriqueApplication = historiqueApplicationsDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<HistoriqueApplicationDTO>>> GetPagedHistoriqueApplications(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.HistoriqueApplications.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var historiqueApplications = await _context.HistoriqueApplications
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.OrderByDescending(h => h.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var historiqueApplicationsDto = _mapper.Map<IEnumerable<HistoriqueApplicationDTO>>(historiqueApplications);
			return Ok(new { HistoriqueApplication = historiqueApplicationsDto, TotalPages = totalPages });
		}

		// GET: api/HistoriqueApplications
		[HttpGet]
        public async Task<ActionResult<IEnumerable<HistoriqueApplicationDTO>>> GetHistoriqueApplications()
        {
			var historiqueApplications = await _context.HistoriqueApplications
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.ToListAsync();

			var historiqueApplicationsDto = _mapper.Map<IEnumerable<HistoriqueApplicationDTO>>(historiqueApplications);
			return Ok(historiqueApplicationsDto);
        }

        // GET: api/HistoriqueApplications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<HistoriqueApplication>> GetHistoriqueApplication(int id)
        {
            var historiqueApplication = await _context.HistoriqueApplications
				.Include(h => h.IdUtilisateurNavigation)
				.ThenInclude(u => u.IdProfilNavigation)
				.FirstOrDefaultAsync(h => h.Id == id);

			if (historiqueApplication == null)
            {
                return NotFound();
            }
			var historiqueApplicationDto = _mapper.Map<HistoriqueApplicationDTO>(historiqueApplication);
			return Ok(historiqueApplicationDto);
        }

        private bool HistoriqueApplicationExists(int id)
        {
            return _context.HistoriqueApplications.Any(e => e.Id == id);
        }
    }
}
