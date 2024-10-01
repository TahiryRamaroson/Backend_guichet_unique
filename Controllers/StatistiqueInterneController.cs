using Backend_guichet_unique.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_guichet_unique.Controllers
{
	[Authorize(Policy = "AdministrateurPolicy")]
	[Route("api/[controller]")]
	[ApiController]
	public class StatistiqueInterneController : ControllerBase
	{
		private readonly GuichetUniqueContext _context;

		public StatistiqueInterneController(GuichetUniqueContext context)
		{
			_context = context;
		}

		[HttpGet("nombreUtilisateur")]
		public async Task<ActionResult<int>> GetNombreUtilisateur()
		{
			var utilisateurs = await _context.Utilisateurs
				.Where(u => u.Statut == 5)
				.ToListAsync();
			return Ok(utilisateurs.Count);
		}

		[HttpGet("nombreMenage")]
		public async Task<ActionResult<int>> GetNombreMenage()
		{
			var menages = await _context.Menages
				.ToListAsync();
			return Ok(menages.Count);
		}

		[HttpGet("nombreRegion")]
		public async Task<ActionResult<int>> GetNombreRegion()
		{
			var regions = await _context.Regions
				.ToListAsync();
			return Ok(regions.Count);
		}

		[HttpGet("nombreDistrict")]
		public async Task<ActionResult<int>> GetNombreDistrict()
		{
			var districts = await _context.Districts
				.ToListAsync();
			return Ok(districts.Count);
		}

		[HttpGet("nombreCommune")]
		public async Task<ActionResult<int>> GetNombreCommune()
		{
			var communes = await _context.Communes
				.ToListAsync();
			return Ok(communes.Count);
		}

		[HttpGet("nombreFokontany")]
		public async Task<ActionResult<int>> GetNombreFokontany()
		{
			var fokontanies = await _context.Fokontanies
				.ToListAsync();
			return Ok(fokontanies.Count);
		}
	}
}
