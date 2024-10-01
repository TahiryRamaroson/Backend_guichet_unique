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
	[Authorize(Policy = "IntervenantOuResponsableOuAdlinistrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class CategoriePlaintesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public CategoriePlaintesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportCategoriePlaintesCSV(ImportDTO import)
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
				while ((line = reader.ReadLine()) != null)
				{
					if (isFirstLine)
					{
						isFirstLine = false;
						continue;
					}
					var values = line.Split(',');
					var categoriePlainte = new CategoriePlainte();

					categoriePlainte.Id = int.Parse(values[0]);
					categoriePlainte.Nom = values[1];

					_context.CategoriePlaintes.Add(categoriePlainte);
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
					var existingCategoriePlainteId = ex.Entries.First().Entity is CategoriePlainte existingCategoriePlainte ? existingCategoriePlainte.Id : (int?)null;
					return Ok(new { error = $"La Categorie_Plainte avec l'id {existingCategoriePlainteId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportCategoriePlaintesExcel(ImportDTO import)
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
				var rows = sheetData.Elements<Row>();

				foreach (var row in sheetData.Elements<Row>().Skip(1))
				{
					var cells = row.Elements<Cell>().ToList();
					var categoriePlainte = new CategoriePlainte();

					categoriePlainte.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					categoriePlainte.Nom = cells.ElementAt(1).CellValue.Text;

					_context.CategoriePlaintes.Add(categoriePlainte);
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
					var existingCategoriePlainteId = ex.Entries.First().Entity is CategoriePlainte existingCategoriePlainte ? existingCategoriePlainte.Id : (int?)null;
					return Ok(new { error = $"La Categorie_Plainte avec l'id {existingCategoriePlainteId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportCategoriePlaintesExcel()
		{
			var categoriePlaintes = await _context.CategoriePlaintes
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
					Name = "Categorie_Plainte"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
				new Cell() { CellValue = new CellValue("Id"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("Nom"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var categoriePlainte in categoriePlaintes)
				{
					var row = new Row();
					row.Append(
					new Cell() { CellValue = new CellValue(categoriePlainte.Id.ToString()), DataType = CellValues.Number },
					new Cell() { CellValue = new CellValue(categoriePlainte.Nom), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "CategoriePlainte" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportCategoriePlaintes()
		{
			var categoriePlaintes = await _context.CategoriePlaintes
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom");

			foreach (var categoriePlainte in categoriePlaintes)
			{
				builder.AppendLine($"{categoriePlainte.Id},{categoriePlainte.Nom}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "CategoriePlainte" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<CategoriePlainte>>> GetFilteredCategoriePlaintes(CategoriePlainteFormDTO categoriePlainteDto, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = categoriePlainteDto.Nom.ToLower();

			var query = _context.CategoriePlaintes
			.Where(c => (c.Nom.ToLower().Contains(text)));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var categoriePlaintes = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { CategoriePlainte = categoriePlaintes, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<CategoriePlainte>>> GetPagedCategoriePlaintes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.CategoriePlaintes.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var categoriePlaintes = await _context.CategoriePlaintes
				.OrderByDescending(c => c.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return Ok(new { CategoriePlainte = categoriePlaintes, TotalPages = totalPages });
		}

		// GET: api/CategoriePlaintes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriePlainte>>> GetCategoriePlaintes()
        {
            return await _context.CategoriePlaintes.ToListAsync();
        }

        // GET: api/CategoriePlaintes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoriePlainte>> GetCategoriePlainte(int id)
        {
            var categoriePlainte = await _context.CategoriePlaintes.FindAsync(id);

            if (categoriePlainte == null)
            {
                return NotFound();
            }

            return categoriePlainte;
        }

        // PUT: api/CategoriePlaintes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategoriePlainte(int id, CategoriePlainteFormDTO categoriePlainteDto)
        {
			var existingCategoriePlainte = await _context.CategoriePlaintes.FindAsync(id);
			if (existingCategoriePlainte == null)
			{
				return NotFound();
			}

			_mapper.Map(categoriePlainteDto, existingCategoriePlainte);

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
                if (!CategoriePlainteExists(id))
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

        // POST: api/CategoriePlaintes
        [HttpPost]
        public async Task<ActionResult<CategoriePlainte>> PostCategoriePlainte(CategoriePlainteFormDTO categoriePlainteDto)
        {
			var categoriePlainte = _mapper.Map<CategoriePlainte>(categoriePlainteDto);
			_context.CategoriePlaintes.Add(categoriePlainte);

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

			return CreatedAtAction("GetCategoriePlainte", new { id = categoriePlainte.Id }, categoriePlainte);
        }

        // DELETE: api/CategoriePlaintes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoriePlainte(int id)
        {
            var categoriePlainte = await _context.CategoriePlaintes.FindAsync(id);
            if (categoriePlainte == null)
            {
                return NotFound();
            }

            _context.CategoriePlaintes.Remove(categoriePlainte);

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

        private bool CategoriePlainteExists(int id)
        {
            return _context.CategoriePlaintes.Any(e => e.Id == id);
        }
    }
}
