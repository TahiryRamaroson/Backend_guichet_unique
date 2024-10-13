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

		[HttpGet("nombreIndividu")]
		public async Task<ActionResult<int>> GetNombreIndividu()
		{
			var individus = await _context.Individus
				.ToListAsync();
			return Ok(individus.Count);
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

		[HttpGet("nombreNaissance")]
		public async Task<ActionResult<int>> GetNombreNaissance()
		{
			var naissances = await _context.Naissances
				.ToListAsync();
			return Ok(naissances.Count);
		}

		[HttpGet("nombreNaissanceValide")]
		public async Task<ActionResult<int>> GetNombreNaissanceValide()
		{
			var naissances = await _context.Naissances
				.Where(n => n.Statut == 5)
				.ToListAsync();
			return Ok(naissances.Count);
		}

		[HttpGet("nombreGrossesse")]
		public async Task<ActionResult<int>> GetNombreGrossesse()
		{
			var grossesses = await _context.Grossesses
				.ToListAsync();
			return Ok(grossesses.Count);
		}

		[HttpGet("nombreGrossesseValide")]
		public async Task<ActionResult<int>> GetNombreGrossesseValide()
		{
			var grossesses = await _context.Grossesses
				.Where(g => g.Statut == 5)
				.ToListAsync();
			return Ok(grossesses.Count);
		}

		[HttpGet("nombreDeces")]
		public async Task<ActionResult<int>> GetNombreDeces()
		{
			var deces = await _context.Deces
				.ToListAsync();
			return Ok(deces.Count);
		}

		[HttpGet("nombreDecesValide")]
		public async Task<ActionResult<int>> GetNombreDecesValide()
		{
			var deces = await _context.Deces
				.Where(d => d.Statut == 5)
				.ToListAsync();
			return Ok(deces.Count);
		}

		[HttpGet("nombrePlainte")]
		public async Task<ActionResult<int>> GetNombrePlainte()
		{
			var plaintes = await _context.Plaintes
				.ToListAsync();
			return Ok(plaintes.Count);
		}

		[HttpGet("nombrePlainteValide")]
		public async Task<ActionResult<int>> GetNombrePlainteValide()
		{
			var plaintes = await _context.Plaintes
				.Where(p => p.Statut == 5)
				.ToListAsync();
			return Ok(plaintes.Count);
		}

		[HttpGet("nombrePlainteNonTraite")]
		public async Task<ActionResult<int>> GetNombrePlainteNonTraite()
		{
			var plaintes = await _context.Plaintes
				.Where(p => p.Statut == 5 && p.StatutTraitement == 0)
				.ToListAsync();
			return Ok(plaintes.Count);
		}

		[HttpGet("nombrePlainteEnCours")]
		public async Task<ActionResult<int>> GetNombrePlainteEnCours()
		{
			var plaintes = await _context.Plaintes
				.Where(p => p.Statut == 5 && p.StatutTraitement == 5)
				.ToListAsync();
			return Ok(plaintes.Count);
		}

		[HttpGet("nombrePlainteTraite")]
		public async Task<ActionResult<int>> GetNombrePlainteTraite()
		{
			var plaintes = await _context.Plaintes
				.Where(p => p.Statut == 5 && p.StatutTraitement == 10)
				.ToListAsync();
			return Ok(plaintes.Count);
		}

		[HttpGet("nombreMigrationEntrante")]
		public async Task<ActionResult<int>> GetNombreMigrationEntrante()
		{
			var entrantes = await _context.MigrationEntrantes
				.ToListAsync();
			return Ok(entrantes.Count);
		}

		[HttpGet("nombreMigrationEntranteValide")]
		public async Task<ActionResult<int>> GetNombreMigrationEntranteValide()
		{
			var entrantes = await _context.MigrationEntrantes
				.Where(m => m.Statut == 5)
				.ToListAsync();
			return Ok(entrantes.Count);
		}

		[HttpGet("nombreMigrationSortante")]
		public async Task<ActionResult<int>> GetNombreMigrationSortante()
		{
			var sortantes = await _context.MigrationSortantes
				.ToListAsync();
			return Ok(sortantes.Count);
		}

		[HttpGet("nombreMigrationSortanteValide")]
		public async Task<ActionResult<int>> GetNombreMigrationSortanteValide()
		{
			var sortantes = await _context.MigrationSortantes
				.Where(m => m.Statut == 5)
				.ToListAsync();
			return Ok(sortantes.Count);
		}


	}
}
