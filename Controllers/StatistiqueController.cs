using AutoMapper;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models.Statistique;
using Backend_guichet_unique.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend_guichet_unique.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StatistiqueController : ControllerBase
	{
		private readonly GuichetUniqueContext _context;
		private readonly IMemoryCache _cache;
		private readonly IConfiguration _configuration;

		public StatistiqueController(GuichetUniqueContext context, IMemoryCache cache, IConfiguration configuration)
		{
			_context = context;
			_cache = cache;
			_configuration = configuration;
		}

		[HttpPost("naissance/nombreParMois")]
		public async Task<ActionResult<List<int>>> GetNaissancesParMois(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			// Initialiser une liste avec 12 zéros pour chaque mois
			var naissancesParMois = Enumerable.Repeat(0, 12).ToList();

			var result = await _context.Naissances
				.Where(n => n.DateNaissance.Year == form.annee)
				.GroupBy(n => n.DateNaissance.Month)
				.Select(g => new
				{
					Mois = g.Key,
					NombreDeNaissances = g.Count()
				})
				.ToListAsync();

			// Mettre à jour la liste avec les résultats de la requête
			foreach (var item in result)
			{
				naissancesParMois[item.Mois - 1] = item.NombreDeNaissances;
			}

			return Ok(naissancesParMois);
		}

		[HttpPost("naissance/nombreParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionData>>> GetNaissancesParRegion(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"NaissancesParRegion_{form.annee}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionData> regions))
			{
				// Initialiser un dictionnaire avec toutes les régions, districts, communes et fokontanys avec 0 naissances
				regions = await _context.Regions
					.Include(r => r.Districts)
						.ThenInclude(d => d.Communes)
							.ThenInclude(c => c.Fokontanies)
					.AsNoTracking()
					.Select(r => new RegionData
					{
						Region = r.Nom,
						Data = 0,
						Districts = r.Districts.Select(d => new DistrictData
						{
							Id = d.Id,
							Nom = d.Nom,
							Communes = d.Communes.Select(c => new CommuneData
								{
									Id = c.Id,
									Nom = c.Nom,
									Fokontanies = c.Fokontanies.Select(f => new FokontanyData
									{
										Id = f.Id,
										Nom = f.Nom,
										Data = 0
									}).ToList()
								}).ToList()
							}).ToList()
						})
					.ToDictionaryAsync(r => r.Region);


				var result = await _context.Naissances
					.Where(n => n.DateNaissance.Year == form.annee)
					.Join(_context.Fokontanies.AsNoTracking(), n => n.IdFokontany, f => f.Id, (n, f) => new { n, f })
					.Join(_context.Communes.AsNoTracking(), nf => nf.f.IdCommune, c => c.Id, (nf, c) => new { nf.n, nf.f, c })
					.Join(_context.Districts.AsNoTracking(), nfc => nfc.c.IdDistrict, d => d.Id, (nfc, d) => new { nfc.n, nfc.f, nfc.c, d })
					.Join(_context.Regions.AsNoTracking(), nfcd => nfcd.d.IdRegion, r => r.Id, (nfcd, r) => new { nfcd.n, nfcd.f, nfcd.c, nfcd.d, r })
					.ToListAsync();

				// Mettre à jour le dictionnaire avec les résultats de la requête
				var regionDict = result.GroupBy(r => r.r.Nom)
					.ToDictionary(g => g.Key, g => g.ToList());

				foreach (var region in regionDict)
				{
					var regionData = regions[region.Key];
					regionData.Data += region.Value.Count;

					foreach (var item in region.Value)
					{
						var district = regionData.Districts.First(d => d.Id == item.d.Id);
						district.Data += 1;

						var commune = district.Communes.First(c => c.Id == item.c.Id);
						commune.Data += 1;

						var fokontany = commune.Fokontanies.First(f => f.Id == item.f.Id);
						fokontany.Data += 1;
					}
				}

				// Définir les options de cache
				var cacheEntryOptions = new MemoryCacheEntryOptions
				{
					Size = 1,
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
					SlidingExpiration = TimeSpan.FromMinutes(5)
				};

				// Enregistrer les données dans le cache
				_cache.Set(cacheKey, regions, cacheEntryOptions);
			}

			return Ok(regions);
		}

		[HttpPost("naissance/view/nombreParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionData>>> GetNaissancesParRegionView(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"NaissancesParRegionView_{form.annee}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionData> regions))
			{
				var result = await _context.NaissancesParFokontanyEtRegions.ToListAsync();

				regions = result
					.GroupBy(r => r.RegionNom)
					.ToDictionary(
						g => g.Key,
						g => new RegionData
						{
							Region = g.Key,
							Data = g.Sum(x => x.NombreNaissancesFokontany),
							Districts = g.GroupBy(d => new { d.DistrictNom})
								.Select(dg => new DistrictData
								{
									Nom = dg.Key.DistrictNom,
									Communes = dg.GroupBy(c => new { c.CommuneNom})
										.Select(cg => new CommuneData
										{
											Nom = cg.Key.CommuneNom,
											Fokontanies = cg.Select(f => new FokontanyData
											{
												Nom = f.FokontanyNom,
												Data = f.NombreNaissancesFokontany
											}).ToList()
										}).ToList()
								}).ToList()
						});

				var cacheEntryOptions = new MemoryCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
					SlidingExpiration = TimeSpan.FromMinutes(5)
				};

				_cache.Set(cacheKey, regions, cacheEntryOptions);
			}

			return Ok(regions);
		}

		[HttpPost("grossesse/nombreParTrancheAge")]
		public async Task<ActionResult> GetGrossessesParTrancheAgeAsync(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var tranches = _configuration.GetSection("Grossesse").Get<GrossesseTrancheAgeSettings>().TrancheAge;
			var result = new List<int>();
			var labels = new List<string>();
			var couleurs = new List<string>();

			foreach (var tranche in tranches)
			{
				couleurs.Add(tranche.Color);
				var min = tranche.Min ?? int.MinValue;
				var max = tranche.Max ?? int.MaxValue;

				var count = await _context.Grossesses
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee)
					.CountAsync();

				if(tranche.Min == null)
				{
					labels.Add($"<{max+1}");
				}
				else if(tranche.Max == null)
				{
					labels.Add($"{min-1}+");
				}
				else
				{
					labels.Add($"{min}-{max}");
				}
				result.Add(count);
			}

			return Ok( new { Series = result, Colors = couleurs, Labels = labels });
		}

		[HttpPost("grossesse/ComplicationParTrancheAge")]
		public async Task<ActionResult> GetComplicationsParTrancheAgeAsync(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var tranches = _configuration.GetSection("Grossesse").Get<GrossesseTrancheAgeSettings>().TrancheAge;
			var result = new List<double>();
			var labels = new List<string>();

			foreach (var tranche in tranches)
			{
				var min = tranche.Min ?? int.MinValue;
				var max = tranche.Max ?? int.MaxValue;

				// Nombre total de grossesses par tranche d'âge
				var totalGrossesses = await _context.Grossesses
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee)
					.CountAsync();

				// Nombre de grossesses avec complications par tranche d'âge
				var complications = await _context.Grossesses
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee && g.RisqueComplication > 20)
					.CountAsync();

				// Calcul du taux de complication
				double tauxComplication = totalGrossesses > 0 ? (double)complications / totalGrossesses * 100 : 0;

				if (tranche.Min == null)
				{
					labels.Add($"<{max + 1}");
				}
				else if (tranche.Max == null)
				{
					labels.Add($"{min - 1}+");
				}
				else
				{
					labels.Add($"{min}-{max}");
				}
				result.Add(tauxComplication);
			}

			return Ok(new { Series = result, Labels = labels });
		}
	}
}
