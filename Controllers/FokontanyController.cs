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

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FokontanyController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;

		public FokontanyController(GuichetUniqueContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
		}

		[HttpPost("import/csv")]
		public async Task<ActionResult> ImportFokontaniesCSV(IFormFile file)
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
					var fokontany = new Fokontany();

					fokontany.Id = int.Parse(values[0]);
					fokontany.Nom = values[1];
					fokontany.IdCommune = int.Parse(values[2]);

					_context.Fokontanies.Add(fokontany);
				}

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
		public async Task<ActionResult> ImportFokontaniesExcel(IFormFile file)
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
					var fokontany = new Fokontany();

					fokontany.Id = int.Parse(cells.ElementAt(0).CellValue.Text);
					fokontany.Nom = cells.ElementAt(1).CellValue.Text;
					fokontany.IdCommune = int.Parse(cells.ElementAt(2).CellValue.Text);

					_context.Fokontanies.Add(fokontany);
				}

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
			.Where(f => (f.Nom.ToLower().Contains(text))
				&& (filtreFokontanyDto.idCommune == -1 || f.IdCommune == filtreFokontanyDto.idCommune));

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
		public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetPagedFokonatnies(int pageNumber = 1)
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

		// GET: api/Fokontany
		[HttpGet]
        public async Task<ActionResult<IEnumerable<FokontanyDTO>>> GetFokontanies()
        {
			var fonkotanies = await _context.Fokontanies
                .Include(f => f.IdCommuneNavigation)
				.ThenInclude(c => c.IdDistrictNavigation)
				.ThenInclude(d => d.IdRegionNavigation)
				.ToListAsync();

			var fonkotaniesDto = _mapper.Map<IEnumerable<FokontanyDTO>>(fonkotanies);
			return Ok(fonkotaniesDto);
		}

        // GET: api/Fokontany/5
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

        // PUT: api/Fokontany/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFokontany(int id, FokontanyFormDTO fokontanyDto)
        {
			var existingFokontany = await _context.Fokontanies.FindAsync(id);
			if (existingFokontany == null)
			{
				return NotFound();
			}

			_mapper.Map(fokontanyDto, existingFokontany);

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

        // POST: api/Fokontany
        [HttpPost]
        public async Task<ActionResult<Fokontany>> PostFokontany(FokontanyFormDTO fokontanyDto)
        {
			var fokontany = _mapper.Map<Fokontany>(fokontanyDto);
			_context.Fokontanies.Add(fokontany);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetFokontany", new { id = fokontany.Id }, fokontany);
        }

        // DELETE: api/Fokontany/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFokontany(int id)
        {
            var fokontany = await _context.Fokontanies.FindAsync(id);
            if (fokontany == null)
            {
                return NotFound();
            }

            _context.Fokontanies.Remove(fokontany);
            await _context.SaveChangesAsync();

			return Ok(new { status = "200" });
		}

        private bool FokontanyExists(int id)
        {
            return _context.Fokontanies.Any(e => e.Id == id);
        }
    }
}
