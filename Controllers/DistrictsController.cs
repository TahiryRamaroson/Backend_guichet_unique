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
	[Authorize(Policy = "AdministrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class DistrictsController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public DistrictsController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		// fonction pour importer un fichier excel des districts en utilisant la bibliothèque DocumentFormat.OpenXml
		[HttpPost("import/excel")]
		public async Task<ActionResult> PostDistrictsExcel(IFormFile file)
		{
			if (file == null || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier XLSX" });
			}
			using (var stream = file.OpenReadStream())
			{
				using (var spreadsheetDocument = SpreadsheetDocument.Open(stream, false))
				{
					var workbookPart = spreadsheetDocument.WorkbookPart;
					var worksheetPart = workbookPart.WorksheetParts.First();
					var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
					foreach (var row in sheetData.Elements<Row>().Skip(1))
					{
						var values = row.Elements<Cell>().Select(c => c.InnerText).ToArray();
						var district = new District
						{
							Id = int.Parse(values[0]),
							Nom = values[1],
							IdRegion = int.Parse(values[2])
						};
						_context.Districts.Add(district);
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
						var existingDistrictId = ex.Entries.First().Entity is District existingDistrict ? existingDistrict.Id : (int?)null;
						return Ok(new { error = $"Le district avec l'id {existingDistrictId} existe déjà" });
					}
				}
			}
			return Ok(new { status = "200" });
		}

		// fonction pour importer un fichier csv des districts
		[HttpPost("import/csv")]
		public async Task<ActionResult> PostDistrictsCsv(IFormFile file)
		{
			if (file == null || !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier CSV" });
			}
			using (var stream = file.OpenReadStream())
			{
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
						var district = new District
						{
							Id = int.Parse(values[0]),
							Nom = values[1],
							IdRegion = int.Parse(values[2])
						};
						_context.Districts.Add(district);
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
						var existingDistrictId = ex.Entries.First().Entity is District existingDistrict ? existingDistrict.Id : (int?)null;
						return Ok(new { error = $"Le district avec l'id {existingDistrictId} existe déjà" });
					}
				}
			}
			return Ok(new { status = "200" });
		}

		// fonction pour exporter un fichier excel des districts en utilisant la bibliothèque DocumentFormat.OpenXml
		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportDistrictsExcel()
		{
			var districts = await _context.Districts
			.Include(d => d.IdRegionNavigation)
			.ToListAsync();

			var stream = new System.IO.MemoryStream();
			using (var workbook = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
			{
				var workbookPart = workbook.AddWorkbookPart();
				workbook.WorkbookPart.Workbook = new Workbook();
				var worksheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
				worksheetPart.Worksheet = new Worksheet(new SheetData());

				var sheets = workbook.WorkbookPart.Workbook.AppendChild(new Sheets());
				var sheet = new Sheet()
				{
					Id = workbook.WorkbookPart.GetIdOfPart(worksheetPart),
					SheetId = 1,
					Name = "Districts"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				sheetData.Append(headerRow);

				var headers = new[] { "Id", "Nom", "IdRegion" };
				foreach (var header in headers)
				{
					var cell = new Cell
					{
						DataType = CellValues.String,
						CellValue = new CellValue(header)
					};
					headerRow.Append(cell);
				}

				foreach (var district in districts)
				{
					var row = new Row();
					sheetData.Append(row);

					var cells = new[]
					{
						new Cell { DataType = CellValues.Number, CellValue = new CellValue(district.Id.ToString()) },
						new Cell { DataType = CellValues.String, CellValue = new CellValue(district.Nom) },
						new Cell { DataType = CellValues.String, CellValue = new CellValue(district.IdRegion) }
					};

					foreach (var cell in cells)
					{
						row.Append(cell);
					}
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "districts" + DateTime.Now.ToString() + ".xlsx"
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

		// fonction pour exporter un fichier csv des districts
		[HttpGet("export/csv")]
		public async Task<ActionResult> ExportDistrictsCsv()
		{
			var districts = await _context.Districts
				.Include(d => d.IdRegionNavigation)
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom,IdRegion");

			foreach (var district in districts)
			{
				builder.AppendLine($"{district.Id},{district.Nom},{district.IdRegion}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "districts" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<District>>> GetFilteredDistricts(FiltreDistrictDTO filtreDistrictDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreDistrictDTO.text.ToLower();

			var query = _context.Districts
			.Include(d => d.IdRegionNavigation)
			.Where(d => (d.Nom.ToLower().Contains(text))
				&& (filtreDistrictDTO.idRegion == -1 || d.IdRegion == filtreDistrictDTO.idRegion));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var districts = await query
			.Include(d => d.IdRegionNavigation)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var districtsDto = _mapper.Map<IEnumerable<DistrictDTO>>(districts);

			return Ok(new { Districts = districtsDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<District>>> GetPagedDistricts(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Districts.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var districts = await _context.Districts
			.Include(d => d.IdRegionNavigation)
			.OrderByDescending(d => d.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var districtsDto = _mapper.Map<IEnumerable<DistrictDTO>>(districts);
			return Ok(new { Districts = districtsDto, TotalPages = totalPages });
		}

		// GET: api/Districts
		[HttpGet]
        public async Task<ActionResult<IEnumerable<District>>> GetDistricts()
        {
			var districts = await _context.Districts
				.Include(d => d.IdRegionNavigation)
				.ToListAsync();

			var districtsDto = _mapper.Map<IEnumerable<DistrictDTO>>(districts);
			return Ok(districtsDto);
		}

        // GET: api/Districts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<District>> GetDistrict(int id)
        {
            var district = await _context.Districts
				.Include(d => d.IdRegionNavigation)
				.FirstOrDefaultAsync(d => d.Id == id);

			if (district == null)
            {
                return NotFound();
            }

			var districtDto = _mapper.Map<DistrictDTO>(district);
			return Ok(districtDto);
		}

        // PUT: api/Districts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDistrict(int id, DistrictFormDTO districtDto)
        {
			var existingDistrict = await _context.Districts.FindAsync(id);
			if (existingDistrict == null)
			{
				return NotFound();
			}

			_mapper.Map(districtDto, existingDistrict);

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
                if (!DistrictExists(id))
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

        // POST: api/Districts
        [HttpPost]
        public async Task<ActionResult<District>> PostDistrict(DistrictFormDTO districtDto)
        {
			var district = _mapper.Map<District>(districtDto);
			_context.Districts.Add(district);

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

			return CreatedAtAction("GetDistrict", new { id = district.Id }, district);
        }

        // DELETE: api/Districts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDistrict(int id)
        {
            var district = await _context.Districts.FindAsync(id);
            if (district == null)
            {
                return NotFound();
            }

            _context.Districts.Remove(district);

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

        private bool DistrictExists(int id)
        {
            return _context.Districts.Any(e => e.Id == id);
        }
    }
}
