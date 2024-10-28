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
using Microsoft.Extensions.Caching.Memory;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsableOuAdlinistrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class FokontanyController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;
		private readonly IMemoryCache _cache;

		public FokontanyController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration, IMemoryCache cache)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
			_cache = cache;
		}

		[HttpGet("menage/{id}")]
		public async Task<ActionResult<FokontanyDTO>> GetFokontanyMenage(int id)
		{
			var fokontany = await _context.Fokontanies
				.FirstOrDefaultAsync(f => f.Id == id);

			if (fokontany == null)
			{
				return NotFound();
			}

			var fokontanyDto = _mapper.Map<FokontanyDTO>(fokontany);
			return fokontanyDto;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportFokontaniesCSV(ImportDTO import)
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
					var fokontany = new Fokontany();

					fokontany.Id = int.Parse(values[0]);
					fokontany.Nom = values[1];
					fokontany.IdCommune = int.Parse(values[2]);

					_context.Fokontanies.Add(fokontany);
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
					var existingFokontanyId = ex.Entries.First().Entity is Fokontany existingFokontany ? existingFokontany.Id : (int?)null;
					return Ok(new { error = $"Le fokontany avec l'id {existingFokontanyId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("import/excel")]
		public async Task<ActionResult> ImportFokontaniesExcel(ImportDTO import)
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
					var fokontany = new Fokontany();

					fokontany.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					fokontany.Nom = cells.ElementAt(1).CellValue.Text;
					fokontany.IdCommune = int.Parse(cells.ElementAt(2).CellValue.Text);

					_context.Fokontanies.Add(fokontany);
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
					var existingFokontanyId = ex.Entries.First().Entity is Fokontany existingFokontany ? existingFokontany.Id : (int?)null;
					return Ok(new { error = $"Le fokontany avec l'id {existingFokontanyId} existe déjà" });
				}
			}

			return Ok(new { status = "200" });
		}

		[HttpGet("export/excel")]
		public async Task<ActionResult> ExportFokontaniesExcel()
		{
			var fokontanies = await _context.Fokontanies
			.Include(f => f.IdCommuneNavigation)
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
					Name = "Fokontany"
				};
				sheets.Append(sheet);

				var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

				var headerRow = new Row();
				headerRow.Append(
				new Cell() { CellValue = new CellValue("Id"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("Nom"), DataType = CellValues.String },
				new Cell() { CellValue = new CellValue("IdCommune"), DataType = CellValues.String }
				);
				sheetData.AppendChild(headerRow);

				foreach (var fokontany in fokontanies)
				{
					var row = new Row();
					row.Append(
					new Cell() { CellValue = new CellValue(fokontany.Id.ToString()), DataType = CellValues.Number },
					new Cell() { CellValue = new CellValue(fokontany.Nom), DataType = CellValues.String },
					new Cell() { CellValue = new CellValue(fokontany.IdCommune), DataType = CellValues.String }
					);
					sheetData.AppendChild(row);
				}
			}

			stream.Position = 0;
			var content = new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
			{
				FileDownloadName = "fokontany" + DateTime.Now.ToString() + ".xlsx"
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
		public async Task<IActionResult> ExportFokontanies()
		{
			var fokontanies = await _context.Fokontanies
				.Include(f => f.IdCommuneNavigation)
				.ToListAsync();

			var builder = new System.Text.StringBuilder();
			builder.AppendLine("Id,Nom,IdCommune");

			foreach (var fokontany in fokontanies)
			{
				builder.AppendLine($"{fokontany.Id},{fokontany.Nom},{fokontany.IdCommune}");
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

			return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "fokontany" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetFilteredFokontanies(FiltreFokontanyDTO filtreFokontanyDto, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreFokontanyDto.text.ToLower();

			var query = _context.Fokontanies
			.Include(f => f.IdCommuneNavigation)
			.ThenInclude(c => c.IdDistrictNavigation)
			.ThenInclude(d => d.IdRegionNavigation)
			.Where(f => f.Nom.ToLower().Contains(text) || f.IdCommuneNavigation.Nom.ToLower().Contains(text) || f.IdCommuneNavigation.IdDistrictNavigation.Nom.ToLower().Contains(text)
					|| 	f.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom.ToLower().Contains(text)
			);

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var fokontanies = await query
			.Include(f => f.IdCommuneNavigation)
			.ThenInclude(c => c.IdDistrictNavigation)
			.ThenInclude(d => d.IdRegionNavigation)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var fokontaniesDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fokontanies);

			return Ok(new { Fokontany = fokontaniesDto, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetPagedFokontanies(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Fokontanies.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var fokontanies = await _context.Fokontanies
				.Include(f => f.IdCommuneNavigation)
				.ThenInclude(c => c.IdDistrictNavigation)
				.ThenInclude(d => d.IdRegionNavigation)
				.OrderByDescending(f => f.Id)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var fokontanyDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fokontanies);
			return Ok(new { Fokontany = fokontanyDto, TotalPages = totalPages });
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetFokontanies()
		{
			var cacheKey = "GetFokontanies";
			if (!_cache.TryGetValue(cacheKey, out IEnumerable<FokontanyDTO> fokontanyDto))
			{
				var fokontanies = await _context.Fokontanies.ToListAsync();
				fokontanyDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fokontanies);

				var cacheEntryOptions = new MemoryCacheEntryOptions()
					.SetSize(1)
					.SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
					.SetSlidingExpiration(TimeSpan.FromMinutes(5));

				_cache.Set(cacheKey, fokontanyDto, cacheEntryOptions);
			}

			return Ok(fokontanyDto);

		}

		//[HttpGet]
		//public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetFokontanies()
		//{
		//	var cacheKey = "GetFokontanies";

		//	// Tentative de récupération dans le cache
		//	if (!_cache.TryGetValue(cacheKey, out IEnumerable<FokontanyDTO> fokontanyDto))
		//	{
		//		// Si le cache est vide, récupération initiale des données
		//		fokontanyDto = await GetAndCacheFokontanies(cacheKey);
		//	}
		//	else
		//	{
		//		// Déclenche le rafraîchissement des données en arrière-plan
		//		_ = RefreshFokontanyCacheAsync(cacheKey);
		//	}

		//	return Ok(fokontanyDto);
		//}

		//private async Task<IEnumerable<FokontanyDTO>> GetAndCacheFokontanies(string cacheKey)
		//{
		//	var fokontanies = await _context.Fokontanies.ToListAsync();
		//	var fokontanyDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fokontanies);

		//	var cacheEntryOptions = new MemoryCacheEntryOptions()
		//		.SetSize(1)
		//		.SetAbsoluteExpiration(TimeSpan.FromDays(1)) // Expiration longue (ajustable selon la rareté des modifications)
		//		.SetPriority(CacheItemPriority.High); // Priorité élevée pour limiter l'éviction automatique

		//	_cache.Set(cacheKey, fokontanyDto, cacheEntryOptions);

		//	return fokontanyDto;
		//}

		//private async Task RefreshFokontanyCacheAsync(string cacheKey)
		//{
		//	// Vérifie si une mise à jour est en cours pour éviter les tâches en double
		//	if (_cache.TryGetValue("RefreshInProgress", out _)) return;

		//	// Indique qu'un rafraîchissement est en cours
		//	_cache.Set("RefreshInProgress", true, TimeSpan.FromMinutes(1));

		//	try
		//	{
		//		await GetAndCacheFokontanies(cacheKey);
		//	}
		//	finally
		//	{
		//		// Supprime le drapeau de rafraîchissement
		//		_cache.Remove("RefreshInProgress");
		//	}
		//}

		[HttpGet("simple")]
		public async Task<ActionResult<IEnumerable<Fokontany>>> GetFokontaniesSimple()
		{
			var fonkotanies = await _context.Fokontanies
				.ToListAsync();

			return Ok(fonkotanies);
		}

		[HttpGet("{id}")]
        public async Task<ActionResult<FokontanyDTO>> GetFokontany(int id)
        {
			var fokontany = await _context.Fokontanies
                .Include(f => f.IdCommuneNavigation)
				.ThenInclude(c => c.IdDistrictNavigation)
				.ThenInclude(d => d.IdRegionNavigation)
				.FirstOrDefaultAsync(f => f.Id == id);

			if (fokontany == null)
            {
                return NotFound();
            }

			var fokontanyDto = _mapper.Map<FokontanyDTO>(fokontany);
			return fokontanyDto;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFokontany(int id, FokontanyFormDTO fokontanyDto)
        {
			var existingFokontany = await _context.Fokontanies.FindAsync(id);
			if (existingFokontany == null)
			{
				return NotFound();
			}

			_mapper.Map(fokontanyDto, existingFokontany);

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
                if (!FokontanyExists(id))
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

        [HttpPost]
        public async Task<ActionResult<Fokontany>> PostFokontany(FokontanyFormDTO fokontanyDto)
        {
			var fokontany = _mapper.Map<Fokontany>(fokontanyDto);
			_context.Fokontanies.Add(fokontany);

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

			return CreatedAtAction("GetFokontany", new { id = fokontany.Id }, fokontany);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFokontany(int id)
        {
            var fokontany = await _context.Fokontanies.FindAsync(id);
            if (fokontany == null)
            {
                return NotFound();
            }

            _context.Fokontanies.Remove(fokontany);

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

        private bool FokontanyExists(int id)
        {
            return _context.Fokontanies.Any(e => e.Id == id);
        }
    }
}
