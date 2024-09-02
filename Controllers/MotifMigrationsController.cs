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
    public class MotifMigrationsController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public MotifMigrationsController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportMotifMigrationsCSV(IFormFile file)
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
					var motifMigration = new MotifMigration();

					motifMigration.Id = int.Parse(values[0]);
					motifMigration.Nom = values[1];

					_context.MotifMigrations.Add(motifMigration);
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
					var existingMotifMigrationId = ex.Entries.First().Entity is MotifMigration existingMotifMigration ? existingMotifMigration.Id : (int?)null;
					return Ok(new { error = $"Le motif de migration avec l'id {existingMotifMigrationId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportMotifMigrationsExcel(IFormFile file)
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
					var motifMigration = new MotifMigration();

					motifMigration.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					motifMigration.Nom = cells.ElementAt(1).CellValue.Text;

					_context.MotifMigrations.Add(motifMigration);
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
					var existingMotifMigrationId = ex.Entries.First().Entity is MotifMigration existingMotifMigration ? existingMotifMigration.Id : (int?)null;
					return Ok(new { error = $"Le motif de migration avec l'id {existingMotifMigrationId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportMotifMigrationsExcel()
		{
			var motifMigrations = await _context.MotifMigrations
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
					Name = "Motif_Migration"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
				new Cell() { CellValue = new CellValue("Id"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("Nom"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var motifMigration in motifMigrations)
				{
					var row = new Row();
					row.Append(
					new Cell() { CellValue = new CellValue(motifMigration.Id.ToString()), DataType = CellValues.Number },
					new Cell() { CellValue = new CellValue(motifMigration.Nom), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "motifMigration" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportMotifMigrations()
		{
			var motifMigrations = await _context.MotifMigrations
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom");

			foreach (var motifMigration in motifMigrations)
			{
				builder.AppendLine($"{motifMigration.Id},{motifMigration.Nom}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "motifMigration" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MotifMigration>>> GetFilteredMotifMigrations(MotifMigrationFormDTO motifMigrationDto, int pageNumber = 1)
		{

			int pageSize = 10;
			var text = motifMigrationDto.Nom.ToLower();

			var query = _context.MotifMigrations
			.Where(m => (m.Nom.ToLower().Contains(text)));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var motifMigrations = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { MotifMigration = motifMigrations, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<MotifMigration>>> GetPagedMotifMigrations(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.MotifMigrations.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var motifMigrations = await _context.MotifMigrations
				.OrderByDescending(m => m.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return Ok(new { MotifMigration = motifMigrations, TotalPages = totalPages });
		}

		// GET: api/MotifMigrations
		[HttpGet]
        public async Task<ActionResult<IEnumerable<MotifMigration>>> GetMotifMigrations()
        {
			return await _context.MotifMigrations.ToListAsync();
        }

        // GET: api/MotifMigrations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MotifMigration>> GetMotifMigration(int id)
        {
            var motifMigration = await _context.MotifMigrations.FindAsync(id);

            if (motifMigration == null)
            {
                return NotFound();
            }

            return motifMigration;
        }

        // PUT: api/MotifMigrations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMotifMigration(int id, MotifMigrationFormDTO motifMigrationDto)
        {
			var existingMotifMigration = await _context.MotifMigrations.FindAsync(id);
			if (existingMotifMigration == null)
			{
				return NotFound();
			}

			_mapper.Map(motifMigrationDto, existingMotifMigration);

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
                if (!MotifMigrationExists(id))
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

        // POST: api/MotifMigrations
        [HttpPost]
        public async Task<ActionResult<MotifMigration>> PostMotifMigration(MotifMigrationFormDTO motifMigrationDto)
        {
			var motifMigration = _mapper.Map<MotifMigration>(motifMigrationDto);
			_context.MotifMigrations.Add(motifMigration);

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = "Ajout";
			historiqueApplication.Composant = this.ControllerContext.ActionDescriptor.ControllerName;
			historiqueApplication.UrlAction = Request.Headers["Referer"].ToString();
			historiqueApplication.DateAction = DateTime.Now;
			historiqueApplication.IdUtilisateur = int.Parse(idu);

			_context.HistoriqueApplications.Add(historiqueApplication);

			await _context.SaveChangesAsync();

            return CreatedAtAction("GetMotifMigration", new { id = motifMigration.Id }, motifMigration);
        }

        // DELETE: api/MotifMigrations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMotifMigration(int id)
        {
            var motifMigration = await _context.MotifMigrations.FindAsync(id);
            if (motifMigration == null)
            {
                return NotFound();
            }

            _context.MotifMigrations.Remove(motifMigration);

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

        private bool MotifMigrationExists(int id)
        {
            return _context.MotifMigrations.Any(e => e.Id == id);
        }
    }
}
