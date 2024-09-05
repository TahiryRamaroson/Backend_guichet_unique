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

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NaissancesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;
		private readonly MegaUploader _megaUploader;
        private readonly IConfiguration _configuration;

		public NaissancesController(GuichetUniqueContext context, IMapper mapper, MegaUploader megaUploader, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_megaUploader = megaUploader;
			_configuration = configuration;
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

		// GET: api/Naissances
		[HttpGet]
        public async Task<ActionResult<IEnumerable<Naissance>>> GetNaissances()
        {
            return await _context.Naissances.ToListAsync();
        }

        // GET: api/Naissances/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Naissance>> GetNaissance(int id)
        {
            var naissance = await _context.Naissances.FindAsync(id);

            if (naissance == null)
            {
                return NotFound();
            }

            return naissance;
        }

        // PUT: api/Naissances/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNaissance(int id, Naissance naissance)
        {
            if (id != naissance.Id)
            {
                return BadRequest();
            }

            _context.Entry(naissance).State = EntityState.Modified;

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

            return NoContent();
        }

        // POST: api/Naissances
        [HttpPost]
        public async Task<ActionResult<Naissance>> PostNaissance(NaissanceFormDTO naissanceDto)
        {
			if (naissanceDto.PieceJustificative.Length > 10485760) // (10 MB)
			{
				return Ok(new { error = "Le fichier est trop volumineux." });
			}

			var naissance = _mapper.Map<Naissance>(naissanceDto);

			var filePath = Path.GetTempFileName();

			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await naissanceDto.PieceJustificative.CopyToAsync(stream);
			}

			await _megaUploader.LoginAsync(_configuration["MegaApi:Email"], _configuration["MegaApi:PWD"]);
			var fileId = await _megaUploader.UploadFileAsync(filePath, "Naissance");
			var shareableLink = await _megaUploader.GetShareableLinkAsync(fileId);
			naissance.PieceJustificative = shareableLink;
			await _megaUploader.LogoutAsync();

			_context.Naissances.Add(naissance);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNaissance", new { id = naissance.Id }, naissance);
        }

        // DELETE: api/Naissances/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNaissance(int id)
        {
            var naissance = await _context.Naissances.FindAsync(id);
            if (naissance == null)
            {
                return NotFound();
            }

            _context.Naissances.Remove(naissance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NaissanceExists(int id)
        {
            return _context.Naissances.Any(e => e.Id == id);
        }
    }
}
