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
		private readonly IConfiguration _configuration;

		public ProfilsController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
		{
			_context = context;
			_mapper = mapper;
			_configuration = configuration;
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
