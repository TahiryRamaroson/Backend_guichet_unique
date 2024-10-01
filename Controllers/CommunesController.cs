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
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml;
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsableOuAdlinistrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class CommunesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public CommunesController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		// fonction pour importer un fichier csv des communes
		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportCommunesCSV(ImportDTO import)
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
					var commune = new Commune();

					commune.Id = int.Parse(values[0]);
					commune.Nom = values[1];
					commune.IdDistrict = int.Parse(values[2]);

					_context.Communes.Add(commune);
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
					var existingCommuneId = ex.Entries.First().Entity is Commune existingCommune ? existingCommune.Id : (int?)null;
					return Ok(new { error = $"La commune avec l'id {existingCommuneId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		// fonction pour importer un fichier excel des communes en utilisant DocumentFormat.OpenXml
		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportCommunesExcel(ImportDTO import)
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
					var commune = new Commune();

					commune.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					commune.Nom = cells.ElementAt(1).CellValue.Text;
					commune.IdDistrict = int.Parse(cells.ElementAt(2).CellValue.Text);

					_context.Communes.Add(commune);
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
					var existingCommuneId = ex.Entries.First().Entity is Commune existingCommune ? existingCommune.Id : (int?)null;
					return Ok(new { error = $"La commune avec l'id {existingCommuneId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		// fonction pour exporter un fichier excel des communes en utilisant DocumentFormat.OpenXml
		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportCommunesExcel()
		{
			var communes = await _context.Communes
			.Include(c => c.IdDistrictNavigation)
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
					Name = "Communes"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
				new Cell() { CellValue = new CellValue("Id"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("Nom"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("IdDistrict"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var commune in communes)
				{
					var row = new Row();
					row.Append(
					new Cell() { CellValue = new CellValue(commune.Id.ToString()), DataType = CellValues.Number },
					new Cell() { CellValue = new CellValue(commune.Nom), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue(commune.IdDistrict), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "communes" + DateTime.Now.ToString() + ".xlsx"
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

		// fonction pour exporter un fichier csv des communes
		[HttpGet("export/csv")]
		public async Task<ActionResult> ExportCommunes()
		{
			var communes = await _context.Communes
				.Include(c => c.IdDistrictNavigation)
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom,IdDistrict");

			foreach (var commune in communes)
			{
				builder.AppendLine($"{commune.Id},{commune.Nom},{commune.IdDistrict}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "communes" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<CommuneDTO>>> GetFilteredDistricts(FiltreCommuneDTO filtreCommuneDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreCommuneDTO.text.ToLower();

			var query = _context.Communes
			.Include(c => c.IdDistrictNavigation)
			.ThenInclude(d => d.IdRegionNavigation)
			.Where(c => c.Nom.ToLower().Contains(text) || c.IdDistrictNavigation.Nom.ToLower().Contains(text) || c.IdDistrictNavigation.IdRegionNavigation.Nom.ToLower().Contains(text));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var communes = await query
			.Include(c => c.IdDistrictNavigation)
			.ThenInclude(d => d.IdRegionNavigation)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var communesDto = _mapper.Map<IEnumerable<CommuneDTO>>(communes);

			return Ok(new { Communes = communesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<CommuneDTO>>> GetPagedCommunes(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Communes.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var communes = await _context.Communes
			.Include(c => c.IdDistrictNavigation)
			.ThenInclude(d => d.IdRegionNavigation)
			.OrderByDescending(c => c.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var communeDto = _mapper.Map<IEnumerable<CommuneDTO>>(communes);
			return Ok(new { Communes = communeDto, TotalPages = totalPages });
		}

		// GET: api/Communes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<CommuneDTO>>> GetCommunes()
        {
			var communes = await _context.Communes
				.Include(c => c.IdDistrictNavigation)
				.ThenInclude(d => d.IdRegionNavigation)
				.ToListAsync();

			var communesDto = _mapper.Map<IEnumerable<CommuneDTO>>(communes);
			return Ok(communesDto);
		}

		// fonction pour obtenir la liste des fokontany d'une commune
		[HttpGet("{id}/fokontany")]
		public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetFokontanyByCommune(int id)
		{
			var fokontany = await _context.Fokontanies
				.Include(f => f.IdCommuneNavigation)
				.Where(f => f.IdCommune == id)
				.ToListAsync();

			var fokontanyDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fokontany);
			return Ok(fokontanyDto);
		}

		// GET: api/Communes/5
		[HttpGet("{id}")]
        public async Task<ActionResult<CommuneDTO>> GetCommune(int id)
        {
			var commune = await _context.Communes
				.Include(c => c.IdDistrictNavigation)
				.ThenInclude(d => d.IdRegionNavigation)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (commune == null)
			{
				return NotFound();
			}

			var communeDto = _mapper.Map<CommuneDTO>(commune);
			return Ok(communeDto);
		}

        // PUT: api/Communes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCommune(int id, CommuneFormDTO communeDto)
        {
			var existingCommune = await _context.Communes.FindAsync(id);
			if (existingCommune == null)
			{
				return NotFound();
			}

			_mapper.Map(communeDto, existingCommune);

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
                if (!CommuneExists(id))
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

        // POST: api/Communes
        [HttpPost]
        public async Task<ActionResult<Commune>> PostCommune(CommuneFormDTO communeDto)
        {
			var commune = _mapper.Map<Commune>(communeDto);
			_context.Communes.Add(commune);

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

            return CreatedAtAction("GetCommune", new { id = commune.Id }, commune);
        }

        // DELETE: api/Communes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommune(int id)
        {
            var commune = await _context.Communes.FindAsync(id);
            if (commune == null)
            {
                return NotFound();
            }

            _context.Communes.Remove(commune);

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

        private bool CommuneExists(int id)
        {
            return _context.Communes.Any(e => e.Id == id);
        }
    }
}
