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
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "AdministrateurPolicy")]
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

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Profil>>> GetFilteredProfils(FiltreProfilDTO filtreProfilDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreProfilDTO.text.ToLower();

			var query = _context.Profils
			.Include(p => p.Utilisateurs)
			.Where(p => p.Nom.ToLower().Contains(text) || p.Description.ToLower().Contains(text));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var profils = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { Profils = profils, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Profil>>> GetPagedProfils(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Profils.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var profils = await _context.Profils
			.Include(p => p.Utilisateurs)
			.OrderByDescending(p => p.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { Profils = profils, TotalPages = totalPages });
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
        public async Task<IActionResult> PutProfil(int id, ProfilDTO profilDto)
        {
			var existingProfil = await _context.Profils.FindAsync(id);
			if (existingProfil == null)
			{
				return NotFound();
			}

			_mapper.Map(profilDto, existingProfil);

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

			return Ok(new { status = "200" });
		}

        [HttpPost]
        public async Task<ActionResult<Profil>> PostProfil(ProfilDTO profilDto)
        {
			var profil = _mapper.Map<Profil>(profilDto);
			_context.Profils.Add(profil);
			try 
            {
				await _context.SaveChangesAsync();
			} 
            catch 
            {
                return Ok(new { error = "Ce profil existe déjà" });
            }

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
            try 
            {
				_context.Profils.Remove(profil);
				await _context.SaveChangesAsync();
			}
            catch 
            {
				return Ok(new { error = "Ce profil ne peut plus être supprimé" });
			}

            return Ok(new {status = "200"});
        }

        private bool ProfilExists(int id)
        {
            return _context.Profils.Any(e => e.Id == id);
        }
    }
}
