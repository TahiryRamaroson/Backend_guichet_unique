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
				.Where(n => n.DateNaissance.Year == form.annee && n.Statut == 5)
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
		public async Task<ActionResult<Dictionary<string, RegionData>>> GetNombreNaissancesParRegion(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"NaissancesParRegion_{form.annee}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionData> regions))
			{
				// Initialiser un dictionnaire avec toutes les régions
				regions = await _context.Regions
					.AsNoTracking()
					.Select(r => new RegionData
					{
						Region = r.Nom,
						Data = 0
						})
					.ToDictionaryAsync(r => r.Region);


				var result = await _context.Naissances
					.Where(n => n.DateNaissance.Year == form.annee && n.Statut == 5)
					.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
					.ThenInclude(c => c.IdDistrictNavigation)
					.ThenInclude(d => d.IdRegionNavigation)
					.ToListAsync();

				// Mettre à jour le dictionnaire avec les résultats de la requête
				var regionDict = result.GroupBy(r => r.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom)
					.ToDictionary(g => g.Key, g => g.ToList());

				foreach (var region in regionDict)
				{
					var regionData = regions[region.Key];
					regionData.Data += region.Value.Count;
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

		[HttpPost("naissance/detailsNombreParRegion/{annee}/{nomRegion}")]
		public async Task<ActionResult<RegionData>> GetDetailsNombreNaissancesParRegion(int annee, string nomRegion)
		{
			if (annee == 0)
			{
				annee = DateTime.Now.Year;
			}

			var cacheKey = $"DetailsNaissancesParRegion_{annee}_{nomRegion}";
			if (!_cache.TryGetValue(cacheKey, out RegionData region))
			{
				// Initialiser le région, districts, communes et fokontanys avec 0 naissances
				region = await _context.Regions
					.Where(r => r.Nom == nomRegion)
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
					.FirstOrDefaultAsync();


				var result = await _context.Naissances
					.Where(n => n.DateNaissance.Year == annee && n.Statut == 5)
					.Join(_context.Fokontanies.AsNoTracking(), n => n.IdFokontany, f => f.Id, (n, f) => new { n, f })
					.Join(_context.Communes.AsNoTracking(), nf => nf.f.IdCommune, c => c.Id, (nf, c) => new { nf.n, nf.f, c })
					.Join(_context.Districts.AsNoTracking(), nfc => nfc.c.IdDistrict, d => d.Id, (nfc, d) => new { nfc.n, nfc.f, nfc.c, d })
					.Join(_context.Regions.AsNoTracking(), nfcd => nfcd.d.IdRegion, r => r.Id, (nfcd, r) => new { nfcd.n, nfcd.f, nfcd.c, nfcd.d, r })
					.ToListAsync();

				foreach(var item in region.Districts.SelectMany(d => d.Communes.SelectMany(c => c.Fokontanies)))
				{
					item.Data = result.Count(r => r.f.Id == item.Id);
				}

				// Définir les options de cache
				var cacheEntryOptions = new MemoryCacheEntryOptions
				{
					Size = 1,
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
					SlidingExpiration = TimeSpan.FromMinutes(5)
				};

				// Enregistrer les données dans le cache
				_cache.Set(cacheKey, region, cacheEntryOptions);
			}

			return Ok(region);
		}

		[HttpPost("naissance/repartitionSexeParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionData>>> GetRepartitionSexeNaissancesParRegion(StatistiqueSexeFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"RepartitionParRegion_{form.annee}_{form.sexe}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionData> regions))
			{
				// Initialiser un dictionnaire avec toutes les régions
				regions = await _context.Regions
					.AsNoTracking()
					.Select(r => new RegionData
					{
						Region = r.Nom,
						Data = 0
					})
					.ToDictionaryAsync(r => r.Region);


				var result = await _context.Naissances
					.Where(n => n.DateNaissance.Year == form.annee && n.Statut == 5 && n.Sexe == form.sexe)
					.Include(n => n.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
					.ThenInclude(c => c.IdDistrictNavigation)
					.ThenInclude(d => d.IdRegionNavigation)
					.ToListAsync();

				// Mettre à jour le dictionnaire avec les résultats de la requête
				var regionDict = result.GroupBy(r => r.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom)
					.ToDictionary(g => g.Key, g => g.ToList());

				foreach (var region in regionDict)
				{
					var regionData = regions[region.Key];
					regionData.Data += region.Value.Count;
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

		[HttpPost("naissance/detailsRepartitionSexeParRegion/{annee}/{nomRegion}/{sexe}")]
		public async Task<ActionResult<RegionData>> GetDetailsRepartitionSexeNaissanceParRegion(int annee, string nomRegion, int sexe)
		{
			if (annee == 0)
			{
				annee = DateTime.Now.Year;
			}

			var cacheKey = $"DetailsRepartitionSexeParRegion_{annee}_{sexe}_{nomRegion}";
			if (!_cache.TryGetValue(cacheKey, out RegionData region))
			{
				// Initialiser le région, districts, communes et fokontanys avec 0 naissances
				region = await _context.Regions
					.Where(r => r.Nom == nomRegion)
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
					.FirstOrDefaultAsync();


				var result = await _context.Naissances
					.Where(n => n.DateNaissance.Year == annee && n.Statut == 5 && n.Sexe == sexe)
					.Join(_context.Fokontanies.AsNoTracking(), n => n.IdFokontany, f => f.Id, (n, f) => new { n, f })
					.Join(_context.Communes.AsNoTracking(), nf => nf.f.IdCommune, c => c.Id, (nf, c) => new { nf.n, nf.f, c })
					.Join(_context.Districts.AsNoTracking(), nfc => nfc.c.IdDistrict, d => d.Id, (nfc, d) => new { nfc.n, nfc.f, nfc.c, d })
					.Join(_context.Regions.AsNoTracking(), nfcd => nfcd.d.IdRegion, r => r.Id, (nfcd, r) => new { nfcd.n, nfcd.f, nfcd.c, nfcd.d, r })
					.ToListAsync();

				foreach (var item in region.Districts.SelectMany(d => d.Communes.SelectMany(c => c.Fokontanies)))
				{
					item.Data = result.Count(r => r.f.Id == item.Id);
				}

				// Définir les options de cache
				var cacheEntryOptions = new MemoryCacheEntryOptions
				{
					Size = 1,
					AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
					SlidingExpiration = TimeSpan.FromMinutes(5)
				};

				// Enregistrer les données dans le cache
				_cache.Set(cacheKey, region, cacheEntryOptions);
			}

			return Ok(region);
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
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee && g.Statut == 5)
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

		[HttpPost("grossesse/complicationParTrancheAge")]
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
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee && g.Statut == 5)
					.CountAsync();

				// Nombre de grossesses avec complications par tranche d'âge
				var complications = await _context.Grossesses
					.Where(g => g.AgeMere >= min && g.AgeMere <= max && g.DateSaisie.Year == form.annee && g.Statut == 5 && g.RisqueComplication > 20)
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

		[HttpPost("migration/fluxParMois")]
		public async Task<ActionResult<List<int>>> GetFluxMigrationParMois(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			// Initialiser une liste avec 12 zéros pour chaque mois
			var migrationEntranteParMois = Enumerable.Repeat(0, 12).ToList();
			var migrationSortanteParMois = Enumerable.Repeat(0, 12).ToList();

			var entrante = await _context.MigrationEntrantes
				.Where(e => e.DateArrivee.Year == form.annee && e.Statut == 5)
				.GroupBy(e => e.DateArrivee.Month)
				.Select(g => new
				{
					Mois = g.Key,
					NombreEntrantes = g.Count()
				})
				.ToListAsync();

			var sortante = await _context.MigrationSortantes
				.Where(e => e.DateDepart.Year == form.annee && e.Statut == 5)
				.GroupBy(e => e.DateDepart.Month)
				.Select(g => new
				{
					Mois = g.Key,
					NombreSortantes = g.Count()
				})
				.ToListAsync();

			// Mettre à jour la liste avec les résultats de la requête
			foreach (var item in entrante)
			{
				migrationEntranteParMois[item.Mois - 1] = item.NombreEntrantes;
			}

			foreach (var item in sortante)
			{
				migrationSortanteParMois[item.Mois - 1] = item.NombreSortantes;
			}

			return Ok(new { Entrantes = migrationEntranteParMois, Sortantes = migrationSortanteParMois });
		}

		[HttpPost("migration/motifPlusFrequent")]
		public async Task<ActionResult<List<MotifMigrationFrequent>>> GetMotifMigrationPlusFrequent(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var motifs = await _context.MigrationSortantes
				.Where(m => m.DateDepart.Year == form.annee)
				.GroupBy(m => new { m.IdMotifMigration, m.IdMotifMigrationNavigation.Nom })
				.Select(g => new MotifMigrationFrequent
				{
					Id = g.Key.IdMotifMigration,
					Nom = g.Key.Nom,
					Count = g.Count()
				})
				.OrderByDescending(m => m.Count)
				.Take(3)
				.ToListAsync();

			return Ok(motifs);
		}

		[HttpPost("plainte/nombreParCategorie")]
		public async Task<ActionResult> GetNombrePlainteParCategorie(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var categories = await _context.CategoriePlaintes.ToListAsync();
			var result = new List<int>();
			var labels = new List<string>();

			foreach (var categorie in categories)
			{
				var count = await _context.Plaintes
					.Where(p => p.IdCategoriePlainte == categorie.Id && p.DateFait.Year == form.annee && p.Statut == 5)
					.CountAsync();

				labels.Add(categorie.Nom);
				result.Add(count);
			}

			return Ok(new { Series = result, Labels = labels });
		}

		[HttpPost("plainte/plusFrequentParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionDataString>>> GetPlaintePlusFrequentParRegion(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"PlaintePlusFrequentParRegion_{form.annee}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionDataString> regions))
			{
				// Initialiser un dictionnaire avec toutes les régions
				regions = await _context.Regions
					.AsNoTracking()
					.Select(r => new RegionDataString
					{
						Region = r.Nom
					})
					.ToDictionaryAsync(r => r.Region);

				var result = await _context.Plaintes
					.Where(p => p.DateFait.Year == form.annee && p.Statut == 5)
					.Include(p => p.IdFokontanyFaitNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
					.ThenInclude(c => c.IdDistrictNavigation)
					.ThenInclude(d => d.IdRegionNavigation)
					.Include(p => p.IdCategoriePlainteNavigation)
					.ToListAsync();

				// Grouper les plaintes par région et par catégorie
				var regionDict = result
					.GroupBy(r => new { Region = r.IdFokontanyFaitNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom, r.IdCategoriePlainteNavigation.Nom })
					.ToDictionary(g => g.Key, g => g.ToList());

				// Dictionnaire temporaire pour stocker les catégories et leurs comptes
				var tempCategories = new Dictionary<string, Dictionary<string, int>>();

				foreach (var region in regionDict)
				{
					var regionName = region.Key.Region;
					var category = region.Key.Nom;
					var count = region.Value.Count;

					if (!tempCategories.ContainsKey(regionName))
					{
						tempCategories[regionName] = new Dictionary<string, int>();
					}

					if (!tempCategories[regionName].ContainsKey(category))
					{
						tempCategories[regionName][category] = 0;
					}
					tempCategories[regionName][category] += count;
				}

				// Trouver la catégorie la plus fréquente pour chaque région et mettre à jour la propriété Data
				foreach (var region in regions)
				{
					if (tempCategories.ContainsKey(region.Key))
					{
						region.Value.Data = tempCategories[region.Key]
							.OrderByDescending(c => c.Value)
							.FirstOrDefault().Key;
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

		[HttpPost("plainte/detailsPlusFrequentParRegion/{annee}/{nomRegion}")]
		public async Task<ActionResult<RegionDataString>> GetDetailsPlusFrequentParRegion(int annee, string nomRegion)
		{
			if (annee == 0)
			{
				annee = DateTime.Now.Year;
			}

			var cacheKey = $"DetailsPlusFrequentParRegion_{annee}_{nomRegion}";
			if (!_cache.TryGetValue(cacheKey, out RegionDataString region))
			{
				// Initialiser le région, districts, communes et fokontanys
				region = await _context.Regions
					.Where(r => r.Nom == nomRegion)
					.Include(r => r.Districts)
						.ThenInclude(d => d.Communes)
							.ThenInclude(c => c.Fokontanies)
					.AsNoTracking()
					.Select(r => new RegionDataString
					{
						Region = r.Nom,
						Districts = r.Districts.Select(d => new DistrictDataString
						{
							Id = d.Id,
							Nom = d.Nom,
							Communes = d.Communes.Select(c => new CommuneDataString
							{
								Id = c.Id,
								Nom = c.Nom,
								Fokontanies = c.Fokontanies.Select(f => new FokontanyDataString
								{
									Id = f.Id,
									Nom = f.Nom
								}).ToList()
							}).ToList()
						}).ToList()
					})
					.FirstOrDefaultAsync();

				// Obtenir les plaintes pour l'année et la région spécifiées
				var plaintes = await _context.Plaintes
					.Where(p => p.DateFait.Year == annee && p.Statut == 5 && p.IdFokontanyFaitNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom == nomRegion)
					.Include(p => p.IdFokontanyFaitNavigation)
					.Include(p => p.IdCategoriePlainteNavigation)
					.ToListAsync();

				// Grouper les plaintes par Fokontany et par catégorie
				var fokontanyDict = plaintes
					.GroupBy(p => new { p.IdFokontanyFait, p.IdCategoriePlainteNavigation.Nom })
					.ToDictionary(g => g.Key, g => g.ToList());

				// Dictionnaire temporaire pour stocker les catégories et leurs comptes pour chaque Fokontany
				var tempCategories = new Dictionary<int, Dictionary<string, int>>();

				foreach (var group in fokontanyDict)
				{
					var fokontanyId = group.Key.IdFokontanyFait;
					var category = group.Key.Nom;
					var count = group.Value.Count;

					if (!tempCategories.ContainsKey(fokontanyId))
					{
						tempCategories[fokontanyId] = new Dictionary<string, int>();
					}

					if (!tempCategories[fokontanyId].ContainsKey(category))
					{
						tempCategories[fokontanyId][category] = 0;
					}
					tempCategories[fokontanyId][category] += count;
				}

				// Mettre à jour la propriété Data avec la catégorie la plus fréquente pour chaque Fokontany
				foreach (var district in region.Districts)
				{
					foreach (var commune in district.Communes)
					{
						foreach (var fokontany in commune.Fokontanies)
						{
							if (tempCategories.ContainsKey(fokontany.Id))
							{
								fokontany.Data = tempCategories[fokontany.Id]
									.OrderByDescending(c => c.Value)
									.FirstOrDefault().Key;
							}
						}
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
				_cache.Set(cacheKey, region, cacheEntryOptions);
			}

			return Ok(region);
		}

		[HttpPost("deces/causeParTrancheAge")]
		public async Task<ActionResult<List<CauseDecesFrequent>>> GetCauseDecesParTrancheAge(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var tranches = _configuration.GetSection("Deces").Get<DecesTrancheAgeSettings>().TrancheAge;
			var result = new List<CauseDecesFrequent>();

			foreach (var tranche in tranches)
			{
				var min = tranche.Min ?? int.MinValue;
				var max = tranche.Max ?? int.MaxValue;

				var causePrincipale = await _context.Deces
					.Where(d => d.AgeDefunt >= min && d.AgeDefunt <= max && d.DateDeces.Year == form.annee && d.Statut == 5)
					.GroupBy(d => d.IdCauseDecesNavigation.Nom)
					.Select(g => new
					{
						CauseDeces = g.Key,
						Count = g.Count()
					})
					.OrderByDescending(g => g.Count)
					.FirstOrDefaultAsync();

				var cause = new CauseDecesFrequent();
				cause.TrancheAge = tranche.Min == null ? $"<{max + 1}" : tranche.Max == null ? $"{min - 1}+" : $"{min}-{max}";

				if (causePrincipale != null)
				{
					cause.CauseDeces = causePrincipale.CauseDeces;
					cause.Count = causePrincipale.Count;
				}
				result.Add(cause);
			}

			return Ok(result);
		}

		[HttpPost("deces/mortaliteParTrancheAge")]
		public async Task<ActionResult> GetMortaliteParTrancheAgeAsync(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var tranches = _configuration.GetSection("Deces").Get<DecesTrancheAgeSettings>().TrancheAge;
			var result = new List<double>();
			var labels = new List<string>();

			foreach (var tranche in tranches)
			{
				var min = tranche.Min ?? int.MinValue;
				var max = tranche.Max ?? int.MaxValue;

				// Nombre total d'individu de cette tranche d'âge
				var totalIndividu = await _context.Individus
					.Where(i => (form.annee - i.DateNaissance.Year) >= min && (form.annee - i.DateNaissance.Year) <= max)
					.CountAsync();

				// Nombre de grossesses avec complications par tranche d'âge
				var totalDeces = await _context.Deces
					.Where(d => d.AgeDefunt >= min && d.AgeDefunt <= max && d.DateDeces.Year == form.annee && d.Statut == 5)
					.CountAsync();

				// Calcul du taux de complication
				double tauxMortalite = totalIndividu > 0 ? (double)totalDeces / totalIndividu * 100 : 0;

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
				result.Add(tauxMortalite);
			}

			return Ok(new { Series = result, Labels = labels });
		}

		[HttpPost("deces/causeParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionDataString>>> GetCauseDecesParRegion(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var regions = await _context.Regions
				.AsNoTracking()
				.Select(r => new RegionDataString
				{
					Region = r.Nom
				})
				.ToDictionaryAsync(r => r.Region);

			var causesParRegion = await _context.Deces
				.Where(d => d.DateDeces.Year == form.annee && d.Statut == 5)
				.Include(d => d.IdCauseDecesNavigation)
				.Include(d => d.IdDefuntNavigation)
				.ThenInclude(i => i.IdMenageNavigation)
				.ThenInclude(d => d.IdFokontanyNavigation)
					.ThenInclude(f => f.IdCommuneNavigation)
					.ThenInclude(c => c.IdDistrictNavigation)
					.ThenInclude(d => d.IdRegionNavigation)
				.GroupBy(d => new
				{
					RegionName = d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom,
					CauseName = d.IdCauseDecesNavigation.Nom
				})
				.Select(g => new
				{
					Region = g.Key.RegionName,
					CauseDeces = g.Key.CauseName,
					Count = g.Count()
				})
				.ToListAsync();

			var groupedByRegion = causesParRegion
				.GroupBy(c => c.Region)
				.Select(g => new
				{
					Region = g.Key,
					CauseDeces = g.OrderByDescending(c => c.Count).First().CauseDeces
				})
				.ToList();

			foreach (var item in groupedByRegion)
			{
				if (regions.ContainsKey(item.Region))
				{
					regions[item.Region].Data = item.CauseDeces;
				}
			}

			return Ok(regions);
		}

		[HttpPost("deces/detailsCauseParRegion/{annee}/{nomRegion}")]
		public async Task<ActionResult<RegionDataString>> GetCauseDecesParRegion(int annee, string nomRegion)
		{
			if (annee == 0)
			{
				annee = DateTime.Now.Year;
			}

			var cacheKey = $"DetailsCauseParRegion_{annee}_{nomRegion}";
			if (!_cache.TryGetValue(cacheKey, out RegionDataString region))
			{
				// Initialiser le région, districts, communes et fokontanys
				region = await _context.Regions
					.Where(r => r.Nom == nomRegion)
					.Include(r => r.Districts)
						.ThenInclude(d => d.Communes)
							.ThenInclude(c => c.Fokontanies)
					.AsNoTracking()
					.Select(r => new RegionDataString
					{
						Region = r.Nom,
						Districts = r.Districts.Select(d => new DistrictDataString
						{
							Id = d.Id,
							Nom = d.Nom,
							Communes = d.Communes.Select(c => new CommuneDataString
							{
								Id = c.Id,
								Nom = c.Nom,
								Fokontanies = c.Fokontanies.Select(f => new FokontanyDataString
								{
									Id = f.Id,
									Nom = f.Nom
								}).ToList()
							}).ToList()
						}).ToList()
					})
					.FirstOrDefaultAsync();

				// Obtenir les décès pour l'année et la région spécifiées
				var deces = await _context.Deces
					.Where(d => d.DateDeces.Year == annee && d.Statut == 5 && d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom == nomRegion)
					.Include(d => d.IdDefuntNavigation)
						.ThenInclude(i => i.IdMenageNavigation)
						.ThenInclude(d => d.IdFokontanyNavigation)
					.Include(d => d.IdCauseDecesNavigation)
					.ToListAsync();

				// Grouper les décès par Fokontany et par cause de décès
				var fokontanyDict = deces
					.GroupBy(d => new { d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.Id, d.IdCauseDecesNavigation.Nom })
					.ToDictionary(g => g.Key, g => g.ToList());

				// Dictionnaire temporaire pour stocker les causes et leurs comptes pour chaque Fokontany
				var tempCauses = new Dictionary<int, Dictionary<string, int>>();

				foreach (var group in fokontanyDict)
				{
					var fokontanyId = group.Key.Id;
					var cause = group.Key.Nom;
					var count = group.Value.Count;

					if (!tempCauses.ContainsKey(fokontanyId))
					{
						tempCauses[fokontanyId] = new Dictionary<string, int>();
					}

					if (!tempCauses[fokontanyId].ContainsKey(cause))
					{
						tempCauses[fokontanyId][cause] = 0;
					}
					tempCauses[fokontanyId][cause] += count;
				}

				// Mettre à jour la propriété Data avec la cause la plus fréquente pour chaque Fokontany
				foreach (var district in region.Districts)
				{
					foreach (var commune in district.Communes)
					{
						foreach (var fokontany in commune.Fokontanies)
						{
							if (tempCauses.ContainsKey(fokontany.Id))
							{
								fokontany.Data = tempCauses[fokontany.Id]
									.OrderByDescending(c => c.Value)
									.FirstOrDefault().Key;
							}
						}
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
				_cache.Set(cacheKey, region, cacheEntryOptions);
			}

			return Ok(region);
		}

		[HttpPost("deces/tauxMortaliteParRegion")]
		public async Task<ActionResult<Dictionary<string, RegionData>>> GetTauxMortaliteParRegion(StatistiqueFormDTO form)
		{
			if (form.annee == 0)
			{
				form.annee = DateTime.Now.Year;
			}

			var cacheKey = $"TauxMortaliteParRegion_{form.annee}";
			if (!_cache.TryGetValue(cacheKey, out Dictionary<string, RegionData> regions))
			{
				// Initialiser un dictionnaire avec toutes les régions
				regions = await _context.Regions
					.AsNoTracking()
					.Select(r => new RegionData
					{
						Region = r.Nom,
						Data = 0
					})
					.ToDictionaryAsync(r => r.Region);

				// Obtenir les décès pour l'année spécifiée
				var deces = await _context.Deces
					.Where(d => d.DateDeces.Year == form.annee && d.Statut == 5)
					.Include(d => d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation)
					.ToListAsync();

				// Obtenir la population pour chaque région
				var populations = await _context.Individus
					.Where(i => i.DateNaissance.Year <= form.annee)
					.Include(i => i.IdMenageNavigation)
						.ThenInclude(m => m.IdFokontanyNavigation)
						.ThenInclude(f => f.IdCommuneNavigation)
						.ThenInclude(c => c.IdDistrictNavigation)
						.ThenInclude(d => d.IdRegionNavigation)
					.GroupBy(i => i.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom)
					.Select(g => new
					{
						Region = g.Key,
						Population = g.Count()
					})
					.ToDictionaryAsync(g => g.Region, g => g.Population);

				// Grouper les décès par région
				var decesParRegion = deces
					.GroupBy(d => d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom)
					.ToDictionary(g => g.Key, g => g.Count());

				// Calculer le taux de mortalité pour chaque région
				foreach (var region in regions)
				{
					if (decesParRegion.ContainsKey(region.Key) && populations.ContainsKey(region.Key))
					{
						var nombreDeces = decesParRegion[region.Key];
						var population = populations[region.Key];
						region.Value.Data = (int)Math.Floor((double)nombreDeces / population * 100);
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

		[HttpPost("deces/detailsTauxMortaliteParRegion/{annee}/{nomRegion}")]
		public async Task<ActionResult<RegionData>> GetDetailsTauxMortaliteParRegion(int annee, string nomRegion)
		{
			if (annee == 0)
			{
				annee = DateTime.Now.Year;
			}

			var cacheKey = $"DetailsTauxMortaliteParRegion_{annee}_{nomRegion}";
			if (!_cache.TryGetValue(cacheKey, out RegionData region))
			{
				// Initialiser le région, districts, communes et fokontanys avec 0 taux de mortalité
				region = await _context.Regions
					.Where(r => r.Nom == nomRegion)
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
					.FirstOrDefaultAsync();

				// Obtenir les décès pour l'année et la région spécifiées
				var deces = await _context.Deces
					.Where(d => d.DateDeces.Year == annee && d.Statut == 5 && d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom == nomRegion)
					.Include(d => d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation)
					.ToListAsync();

				// Obtenir la population pour chaque Fokontany
				var populations = await _context.Individus
					.Where(i => i.DateNaissance.Year <= annee && i.IdMenageNavigation.IdFokontanyNavigation.IdCommuneNavigation.IdDistrictNavigation.IdRegionNavigation.Nom == nomRegion)
					.Include(i => i.IdMenageNavigation.IdFokontanyNavigation)
					.GroupBy(i => i.IdMenageNavigation.IdFokontanyNavigation.Nom)
					.Select(g => new
					{
						Fokontany = g.Key,
						Population = g.Count()
					})
					.ToDictionaryAsync(g => g.Fokontany, g => g.Population);

				// Grouper les décès par Fokontany
				var decesParFokontany = deces
					.GroupBy(d => d.IdDefuntNavigation.IdMenageNavigation.IdFokontanyNavigation.Nom)
					.ToDictionary(g => g.Key, g => g.Count());

				// Calculer le taux de mortalité pour chaque Fokontany
				foreach (var item in region.Districts.SelectMany(d => d.Communes.SelectMany(c => c.Fokontanies)))
				{
					if (decesParFokontany.ContainsKey(item.Nom) && populations.ContainsKey(item.Nom))
					{
						var nombreDeces = decesParFokontany[item.Nom];
						var population = populations[item.Nom];
						Console.WriteLine($"{item.Nom} : {nombreDeces} / {population}");
						item.Data = (int)Math.Floor((double)nombreDeces / population * 100);
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
				_cache.Set(cacheKey, region, cacheEntryOptions);
			}

			return Ok(region);
		}



	}
}
