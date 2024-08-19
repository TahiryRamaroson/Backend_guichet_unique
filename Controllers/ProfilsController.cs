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

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfilsController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;

		public ProfilsController(GuichetUniqueContext context, IMapper mapper)
        {
            _context = context;
			_mapper = mapper;

		}

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Profil>>> GetProfils()
        {
			var profils = await _context.Profils
		        .Include(p => p.Utilisateurs)
		        .ToListAsync();

			return profils;
		}

        [HttpGet("{id}")]
        public async Task<ActionResult<Profil>> GetProfil(int id)
        {
            var profil = await _context.Profils.FindAsync(id);

            if (profil == null)
            {
                return NotFound();
            }

            return profil;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProfil(int id, Profil profil)
        {
            if (id != profil.Id)
            {
                return BadRequest();
            }

            _context.Entry(profil).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProfilExists(id))
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

        [HttpPost]
        public async Task<ActionResult<Profil>> PostProfil(ProfilDTO profilDto)
        {
			var profil = _mapper.Map<Profil>(profilDto);
			_context.Profils.Add(profil);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProfil", new { id = profil.Id }, profil);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProfil(int id)
        {
            var profil = await _context.Profils.FindAsync(id);
            if (profil == null)
            {
                return NotFound();
            }

            _context.Profils.Remove(profil);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProfilExists(int id)
        {
            return _context.Profils.Any(e => e.Id == id);
        }
    }
}
