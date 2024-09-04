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
using DocumentFormat.OpenXml.ExtendedProperties;

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenagesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
        private readonly IMapper _mapper;

		public MenagesController(GuichetUniqueContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;
		}

        // GET: api/Menages
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MenageDTO>>> GetMenages()
        {
			var menages = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
				    .ThenInclude(f => f.IdCommuneNavigation)
					    .ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.ToListAsync();

			var menagesDto = _mapper.Map<IEnumerable<MenageDTO>>(menages);
			return Ok(menagesDto);
        }

		[HttpGet("numero/{numeroMenage}")]
		public async Task<ActionResult<MenageDTO>> GetMenage(string numeroMenage)
		{
			var menage = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.FirstOrDefaultAsync(m => m.NumeroMenage == numeroMenage);

			if (menage == null)
			{
				return NotFound();
			}

			var menageDto = _mapper.Map<MenageDTO>(menage);
			return Ok(menageDto);
		}

		// GET: api/Menages/5
		[HttpGet("{id}")]
        public async Task<ActionResult<MenageDTO>> GetMenage(int id)
        {
			var menage = await _context.Menages
				.Include(m => m.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
							.ThenInclude(d => d.IdRegionNavigation)
				.Include(m => m.Individus)
				.FirstOrDefaultAsync(m => m.Id == id);

            if (menage == null)
            {
				return NotFound();
			}

			var menageDto = _mapper.Map<MenageDTO>(menage);
			return Ok(menageDto);
		}

		// PUT: api/Menages/5
		[HttpPut("{id}")]
        private async Task<IActionResult> PutMenage(int id, Menage menage)
        {
            if (id != menage.Id)
            {
                return BadRequest();
            }

            _context.Entry(menage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MenageExists(id))
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

        // POST: api/Menages
        [HttpPost]
        private async Task<ActionResult<Menage>> PostMenage(Menage menage)
        {
            _context.Menages.Add(menage);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMenage", new { id = menage.Id }, menage);
        }

        // DELETE: api/Menages/5
        [HttpDelete("{id}")]
        private async Task<IActionResult> DeleteMenage(int id)
        {
            var menage = await _context.Menages.FindAsync(id);
            if (menage == null)
            {
                return NotFound();
            }

            _context.Menages.Remove(menage);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MenageExists(int id)
        {
            return _context.Menages.Any(e => e.Id == id);
        }
    }
}
