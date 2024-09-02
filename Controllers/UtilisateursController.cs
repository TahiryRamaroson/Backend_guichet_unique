using System;
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
using Microsoft.AspNetCore.Authorization;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "AdministrateurPolicy")]
	[Route("api/[controller]")]
    [ApiController]
    public class UtilisateursController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;
		private readonly IMapper _mapper;
		private readonly IConfiguration _configuration;

		public UtilisateursController(GuichetUniqueContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
			_configuration = configuration;
		}

		[HttpPost("filtre/page/{pageNumber}")]
		public async Task<ActionResult<IEnumerable<Utilisateur>>> GetFilteredUtilisateurs(FiltreUtilisateurDTO filtreUtilisateurDTO, int pageNumber = 1)
		{
			int pageSize = 10;
			var text = filtreUtilisateurDTO.text.ToLower();

			var query = _context.Utilisateurs
			.Include(u => u.IdProfilNavigation)
			.Where(u => (u.Nom.ToLower().Contains(text) || u.Prenom.ToLower().Contains(text) || u.Matricule.ToLower().Contains(text))
				&& (filtreUtilisateurDTO.idProfil == -1 || u.IdProfil == filtreUtilisateurDTO.idProfil)
				&& (filtreUtilisateurDTO.statut == -1 || u.Statut == filtreUtilisateurDTO.statut));

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var utilisateurs = await query
			.Include(u => u.IdProfilNavigation)
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

			var utilisateursDto = _mapper.Map<IEnumerable<UtilisateurDTO>>(utilisateurs);

			return Ok(new { Utilisateurs = utilisateursDto, TotalPages = totalPages });
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

			var token = Request.Headers["Authorization"].ToString().Substring(7);
			var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var jsonToken = handler.ReadToken(token) as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;
			var idu = jsonToken.Claims.First(claim => claim.Type == "idutilisateur").Value;

			var historiqueApplication = new HistoriqueApplication();
			historiqueApplication.Action = _configuration["Action:Creation"];
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
				return Ok(new { error = "Email déjà utilisé par un utilisateur" });
			}

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
