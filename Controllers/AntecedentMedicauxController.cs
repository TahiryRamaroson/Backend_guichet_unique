using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "AdministrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class AntecedentMedicauxController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public AntecedentMedicauxController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportAntecedentMedicauxCSV(IFormFile file)
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
				while ((line = reader.ReadLine()) != null)
				{
					if (isFirstLine)
					{
						isFirstLine = false;
						continue;
					}
					var values = line.Split(',');
					var antecedentMedicaux = new AntecedentMedicaux();

					antecedentMedicaux.Id = int.Parse(values[0]);
					antecedentMedicaux.Nom = values[1];

					_context.AntecedentMedicauxes.Add(antecedentMedicaux);
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
					var existingAntecedentMedicauxId = ex.Entries.First().Entity is AntecedentMedicaux existingAntecedentMedicaux ? existingAntecedentMedicaux.Id : (int?)null;
					return Ok(new { error = $"L'Antécedent Medicaux avec l'id {existingAntecedentMedicauxId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportAntecedentMedicauxExcel(IFormFile file)
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
				var rows = sheetData.Elements<Row>();

				foreach (var row in sheetData.Elements<Row>().Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var antecedentMedicaux = new AntecedentMedicaux();

					antecedentMedicaux.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					antecedentMedicaux.Nom = cells.ElementAt(1).CellValue.Text;

					_context.AntecedentMedicauxes.Add(antecedentMedicaux);
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
					var existingAntecedentMedicauxId = ex.Entries.First().Entity is AntecedentMedicaux existingAntecedentMedicaux ? existingAntecedentMedicaux.Id : (int?)null;
					return Ok(new { error = $"L' Antécedent Medicaux avec l'id {existingAntecedentMedicauxId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportAntecedentMedicauxExcel()
		{
			var antecedentMedicauxes = await _context.AntecedentMedicauxes
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
					Name = "Antecedent_Medicaux"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
				new Cell() { CellValue = new CellValue("Id"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("Nom"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var antecedentMedicaux in antecedentMedicauxes)
				{
					var row = new Row();
					row.Append(
					new Cell() { CellValue = new CellValue(antecedentMedicaux.Id.ToString()), DataType = CellValues.Number },
					new Cell() { CellValue = new CellValue(antecedentMedicaux.Nom), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "antecedentMedicaux" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportAntecedentMedicaux()
		{
			var antecedentMedicauxes = await _context.AntecedentMedicauxes
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom");

			foreach (var antecedentMedicaux in antecedentMedicauxes)
			{
				builder.AppendLine($"{antecedentMedicaux.Id},{antecedentMedicaux.Nom}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "antecedentMedicaux" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<AntecedentMedicaux>>> GetFilteredAntecedentMedicaux(AntecedentMedicauxFormDTO antecedentMedicauxDto, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = antecedentMedicauxDto.Nom.ToLower();

			var query = _context.AntecedentMedicauxes
			.Where(a => (a.Nom.ToLower().Contains(text)));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var antecedentMedicauxes = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { AntecedentMedicaux = antecedentMedicauxes, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<AntecedentMedicaux>>> GetPagedAntecedentMedicaux(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.AntecedentMedicauxes.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var antecedentMedicauxes = await _context.AntecedentMedicauxes
				.OrderByDescending(a => a.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return Ok(new { AntecedentMedicaux = antecedentMedicauxes, TotalPages = totalPages });
		}

		// GET: api/AntecedentMedicaux
		[HttpGet]
        public async Task<ActionResult<IEnumerable<AntecedentMedicaux>>> GetAntecedentMedicauxes()
        {
            return await _context.AntecedentMedicauxes.ToListAsync();
        }

        // GET: api/AntecedentMedicaux/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AntecedentMedicaux>> GetAntecedentMedicaux(int id)
        {
            var antecedentMedicaux = await _context.AntecedentMedicauxes.FindAsync(id);

            if (antecedentMedicaux == null)
            {
                return NotFound();
            }

            return antecedentMedicaux;
        }

        // PUT: api/AntecedentMedicaux/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAntecedentMedicaux(int id, AntecedentMedicauxFormDTO antecedentMedicauxDto)
        {
			var existingAntecedentMedicaux = await _context.AntecedentMedicauxes.FindAsync(id);
			if (existingAntecedentMedicaux == null)
			{
				return NotFound();
			}

			_mapper.Map(antecedentMedicauxDto, existingAntecedentMedicaux);

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Update"];
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
                if (!AntecedentMedicauxExists(id))
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

        // POST: api/AntecedentMedicaux
        [HttpPost]
        public async Task<ActionResult<AntecedentMedicaux>> PostAntecedentMedicaux(AntecedentMedicauxFormDTO antecedentMedicauxDto)
        {
			var antecedentMedicaux = _mapper.Map<AntecedentMedicaux>(antecedentMedicauxDto);
			_context.AntecedentMedicauxes.Add(antecedentMedicaux);

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

            return CreatedAtAction("GetAntecedentMedicaux", new { id = antecedentMedicaux.Id }, antecedentMedicaux);
        }

        // DELETE: api/AntecedentMedicaux/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAntecedentMedicaux(int id)
        {
            var antecedentMedicaux = await _context.AntecedentMedicauxes.FindAsync(id);
            if (antecedentMedicaux == null)
            {
                return NotFound();
            }

            _context.AntecedentMedicauxes.Remove(antecedentMedicaux);

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Delete"];
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

        private bool AntecedentMedicauxExists(int id)
        {
            return _context.AntecedentMedicauxes.Any(e => e.Id == id);
        }
    }
}
