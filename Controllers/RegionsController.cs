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
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "IntervenantOuResponsableOuAdlinistrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class RegionsController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public RegionsController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
		}

		// fonction qui reçoit un fichier csv des regions et les ajoute à la base de données
		[HttpPost("import/csv")]
		public async Task<ActionResult> PostRegionsCsv(ImportDTO import)
		{
			if (import.Fichier == null || !import.Fichier.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier CSV" });
			}
			using (var stream = import.Fichier.OpenReadStream())
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
						var region = new Region
						{
							Id = int.Parse(values[0]),
							Nom = values[1]
						};
						_context.Regions.Add(region);
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
						var existingRegionId = ex.Entries.First().Entity is Region existingRegion ? existingRegion.Id : (int?)null;
						return Ok(new { error = $"La région avec l'id {existingRegionId} existe déjà" });
					}
				}
			}
			return Ok(new { status = "200" });
		}

		// fonction qui reçoit un fichier excel des regions et les ajoute à la base de données
		[HttpPost("import/excel")]
		public async Task<ActionResult> PostRegionsExcel(ImportDTO import)
		{
			if (import.Fichier == null || !import.Fichier.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return Ok(new { error = "Le fichier doit être un fichier XLSX" });
			}
			using (var stream = import.Fichier.OpenReadStream())
			{
				using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream, false))
				{
					var workbookPart = document.WorkbookPart;
					var worksheetPart = workbookPart.WorksheetParts.First();
					var sheetData = worksheetPart.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.SheetData>().First();
					foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().Skip(1))
					{
						var cells = row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
						var region = new Region
						{
							Id = int.Parse(cells[0].CellValue.Text),
							Nom = cells[1].CellValue.Text
						};
						_context.Regions.Add(region);
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
						var existingRegionId = ex.Entries.First().Entity is Region existingRegion ? existingRegion.Id : (int?)null;
						return Ok(new { error = $"La région avec l'id {existingRegionId} existe déjà" });
					}
				}
			}
			return Ok(new { status = "200" });
		}

		// fonction pour obtenir un fichier excel des regions en utilisant DocumentFormat.OpenXml
		[HttpGet("export/excel")]
		public async Task<ActionResult> GetRegionsExcel()
		{
			var regions = await _context.Regions.ToListAsync();
			var stream = new System.IO.MemoryStream();

			using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
			{
				var workbookPart = document.AddWorkbookPart();
				workbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();

				var worksheetPart = workbookPart.AddNewPart<DocumentFormat.OpenXml.Packaging.WorksheetPart>();
				worksheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet();

				var sheetData = new DocumentFormat.OpenXml.Spreadsheet.SheetData();
				worksheetPart.Worksheet.Append(sheetData);

				var sheets = workbookPart.Workbook.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
				var sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet()
				{
					Id = workbookPart.GetIdOfPart(worksheetPart),
					SheetId = 1,
					Name = "Sheet1"
				};
				sheets.Append(sheet);

				var headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
				headerRow.Append(
				new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Id"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String },
				new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue("Nom"), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String }
				);
				sheetData.Append(headerRow);

				foreach (var region in regions)
				{
					var row = new DocumentFormat.OpenXml.Spreadsheet.Row();
					row.Append(
					new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(region.Id.ToString()), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.Number },
					new DocumentFormat.OpenXml.Spreadsheet.Cell() { CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(region.Nom), DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String }
					);
					sheetData.Append(row);
				}
			}

			stream.Position = 0;

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

			return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "regions" + DateTime.Now.ToString() + ".xlsx");
		}

		//fonction pour obtenir un fichier csv des regions
		[HttpGet("export/csv")]
		public async Task<ActionResult> GetRegionsCsv()
		{
			var regions = await _context.Regions.ToListAsync();
			var csv = "Id,Nom\n";
			foreach (var region in regions)
			{
				csv += $"{region.Id},{region.Nom}\n";
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

			return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "regions" + DateTime.Now.ToString() + ".csv");
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Region>>> GetFilteredRegions(RegionDTO filtreRegionDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreRegionDTO.Nom.ToLower();

			var query = _context.Regions
			.Where(p => p.Nom.ToLower().Contains(text));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var regions = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { Regions = regions, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Region>>> GetPagedRegions(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Regions.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var regions = await _context.Regions
			.OrderByDescending(p => p.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { Regions = regions, TotalPages = totalPages });
		}

		// GET: api/Regions
		[HttpGet]
        public async Task<ActionResult<IEnumerable<Region>>> GetRegions()
        {
            return await _context.Regions.ToListAsync();
        }

		// fonction pour avoir la liste des districts d'une région
		[HttpGet("{id}/districts")]
		public async Task<ActionResult<IEnumerable<District>>> GetDistricts(int id)
		{
			var region = await _context.Regions.FindAsync(id);
			if (region == null)
			{
				return NotFound();
			}

			return await _context.Districts.Where(d => d.IdRegion == id).ToListAsync();
		}

		// GET: api/Regions/5
		[HttpGet("{id}")]
        public async Task<ActionResult<Region>> GetRegion(int id)
        {
            var region = await _context.Regions.FindAsync(id);

            if (region == null)
            {
                return NotFound();
            }

            return region;
        }

        // PUT: api/Regions/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRegion(int id, RegionDTO regionDto)
        {
			var existingRegion = await _context.Regions.FindAsync(id);
			if (existingRegion == null)
			{
				return NotFound();
			}

			_mapper.Map(regionDto, existingRegion);

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
                if (!RegionExists(id))
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

        // POST: api/Regions
        [HttpPost]
        public async Task<ActionResult<Region>> PostRegion(RegionDTO regionDto)
        {
			var region = _mapper.Map<Region>(regionDto);
			_context.Regions.Add(region);

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

			return CreatedAtAction("GetRegion", new { id = region.Id }, region);
        }

        // DELETE: api/Regions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegion(int id)
        {
            var region = await _context.Regions.FindAsync(id);
            if (region == null)
            {
                return NotFound();
            }

            _context.Regions.Remove(region);

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

        private bool RegionExists(int id)
        {
            return _context.Regions.Any(e => e.Id == id);
        }
    }
}
