﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using AutoMapper;
using System.Text;

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UtilisateursController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;

		public UtilisateursController(GuichetUniqueContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Profil>>> GetFilteredUtilisateurs(FiltreUtilisateurDTO filtreUtilisateurDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreUtilisateurDTO.text.ToLower();

			var query = _context.Utilisateurs
			.Include(p => p.IdProfilNavigation)
			.Where(p => (p.Nom.ToLower().Contains(text) || p.Prenom.ToLower().Contains(text) || p.Matricule.ToLower().Contains(text))
				&& (filtreUtilisateurDTO.idProfil == -1 || p.IdProfil == filtreUtilisateurDTO.idProfil)
				&& (filtreUtilisateurDTO.statut == -1 || p.Statut == filtreUtilisateurDTO.statut));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var utilisateurs = await query
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			return Ok(new { Utilisateurs = utilisateurs, TotalPages = totalPages });
		}

		[HttpGet("page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Utilisateur>>> GetPagedUtilisateurs(int pageNumber = 1)
		{
			int pageSize = 10;
			var totalItems = await _context.Utilisateurs.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var utilisateurs = await _context.Utilisateurs
			.Include(u => u.IdProfilNavigation)
			.OrderByDescending(u => u.Id)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var utilisateursDto = _mapper.Map<IEnumerable<UtilisateurDTO>>(utilisateurs);
			return Ok(new { Utilisateurs = utilisateursDto, TotalPages = totalPages });
		}

		// GET: api/Utilisateurs
		[HttpGet]
        public async Task<ActionResult<IEnumerable<Utilisateur>>> GetUtilisateurs()
        {
			var utilisateurs = await _context.Utilisateurs
		        .Include(u => u.IdProfilNavigation)
		        .ToListAsync();

			var utilisateursDto = _mapper.Map<IEnumerable<UtilisateurDTO>>(utilisateurs);
			return Ok(utilisateursDto);
		}

        // GET: api/Utilisateurs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Utilisateur>> GetUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs
				.Include(u => u.IdProfilNavigation)
				.FirstOrDefaultAsync(u => u.Id == id);

			if (utilisateur == null)
            {
                return NotFound();
            }

			var utilisateursDto = _mapper.Map<UtilisateurDTO>(utilisateur);

			return Ok(utilisateursDto);
        }

        // PUT: api/Utilisateurs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUtilisateur(int id, UtilisateurPutDTO utilisateurDto)
        {

			var existingUtilisateur = await _context.Utilisateurs.FindAsync(id);
			if (existingUtilisateur == null)
			{
				return NotFound();
			}

			_mapper.Map(utilisateurDto, existingUtilisateur);

			try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UtilisateurExists(id))
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

        // POST: api/Utilisateurs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Utilisateur>> PostUtilisateur(UtilisateurPutDTO utilisateurDto)
        {
			var command = _context.Database.GetDbConnection().CreateCommand();
			command.CommandText = "SELECT nextval('seq_matricule_utilisateur')";
			await _context.Database.OpenConnectionAsync();

			var matricule = (long)await command.ExecuteScalarAsync();
			Console.WriteLine(matricule + "---------------------------------------");

			var utilisateur = _mapper.Map<Utilisateur>(utilisateurDto);

			utilisateur.Matricule = "UTILISATEUR" + matricule.ToString("D6");
            utilisateur.MotDePasse = GetHashSha256(utilisateur.MotDePasse);

			_context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUtilisateur", new { id = utilisateur.Id }, utilisateur);
        }

        // DELETE: api/Utilisateurs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUtilisateur(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
            {
                return NotFound();
            }

            _context.Utilisateurs.Remove(utilisateur);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UtilisateurExists(int id)
        {
            return _context.Utilisateurs.Any(e => e.Id == id);
        }

		public static string GetHashSha256(string text)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(text);
			SHA256Managed hashstring = new SHA256Managed();
			byte[] hash = hashstring.ComputeHash(bytes);
			string hashString = string.Empty;
			foreach (byte x in hash)
			{
				hashString += String.Format("{0:x2}", x);
			}
			return hashString;
		}
	}
}
