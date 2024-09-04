using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_guichet_unique.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Trainers;
using System.Numerics;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Backend_guichet_unique.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaintesController : ControllerBase
    {
        private readonly GuichetUniqueContext _context;

        public PlaintesController(GuichetUniqueContext context)
        {
            _context = context;
        }

		[HttpPost("entrainement")]
		public async Task<ActionResult> EntrainerModel()
		{
			// Charger le contexte de machine learning
			var mlContext = new MLContext();

			// Charger les données d'entraînement
			var data = mlContext.Data.LoadFromTextFile<PlainteModel>("data_entrainement_plainte.csv", hasHeader: true, separatorChar: ',');

			// Créer un pipeline de transformation de données et d'entraînement
			var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", new TextFeaturizingEstimator.Options
			{
				OutputTokensColumnName = "Tokens",
				CaseMode = TextNormalizingEstimator.CaseMode.Lower,
				KeepDiacritics = false,
				KeepPunctuations = false,
				StopWordsRemoverOptions = new StopWordsRemovingEstimator.Options
				{
					Language = TextFeaturizingEstimator.Language.French
				},
				WordFeatureExtractor = new WordBagEstimator.Options
				{
					NgramLength = 1,
					UseAllLengths = true,
					Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf
				}
			}, nameof(PlainteModel.Description))
			.Append(mlContext.Transforms.Conversion.MapValueToKey("Label"))
			.Append(mlContext.Transforms.Concatenate("Features", "Features"))
			.Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
			.Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

			// Entraîner le modèle
			ITransformer model = pipeline.Fit(data);

			// Enregistrer le modèle
			using (var fileStream = new FileStream("model.zip", FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				mlContext.Model.Save(model, data.Schema, fileStream);
			}

			return Ok(new { status = "200" });
		}

		[HttpPost("prediction")]
		public async Task<ActionResult<CategoriePlainte>> PredictCategory([FromBody] string desc)
		{
			try
			{
				// Charger le contexte de machine learning
				var mlContext = new MLContext();

				// Charger le modèle entraîné (à faire une seule fois et enregistrer le modèle)
				ITransformer model = mlContext.Model.Load("model.zip", out var modelInputSchema);

				// Créer un moteur de prédiction
				var predictionEngine = mlContext.Model.CreatePredictionEngine<PlainteModel, PlaintePrediction>(model);

				// Créer une instance de PlainteModel pour la prédiction
				var plainte = new PlainteModel { Description = desc };

				Console.WriteLine($"Description : ----------------------------- {plainte.Description} -----------------------------");

				// Prédire la catégorie de la plainte
				var prediction = predictionEngine.Predict(plainte);

				Console.WriteLine($"Prédiction : ----------------------------- {prediction.PredictedLabel} -----------------------------");

				// Vérifier la prédiction
				if (prediction == null)
				{
					return BadRequest("La prédiction a échoué.");
				}

				var categoriePlainte = await _context.CategoriePlaintes.FindAsync((int)prediction.PredictedLabel);

				if (categoriePlainte == null)
				{
					return NotFound();
				}

				return Ok(categoriePlainte);
			}
			catch (Exception ex)
			{
				// Enregistrer l'erreur
				Console.WriteLine($"Erreur lors de la prédiction : {ex.Message}");
				return StatusCode(500, "Erreur interne du serveur.");
			}
		}

		// GET: api/Plaintes
		[HttpGet]
        public async Task<ActionResult<IEnumerable<Plainte>>> GetPlaintes()
        {
            return await _context.Plaintes.ToListAsync();
        }

        // GET: api/Plaintes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Plainte>> GetPlainte(int id)
        {
            var plainte = await _context.Plaintes.FindAsync(id);

            if (plainte == null)
            {
                return NotFound();
            }

            return plainte;
        }

        // PUT: api/Plaintes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlainte(int id, Plainte plainte)
        {
            if (id != plainte.Id)
            {
                return BadRequest();
            }

            _context.Entry(plainte).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlainteExists(id))
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

        // POST: api/Plaintes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Plainte>> PostPlainte(Plainte plainte)
        {
            _context.Plaintes.Add(plainte);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlainte", new { id = plainte.Id }, plainte);
        }

        // DELETE: api/Plaintes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlainte(int id)
        {
            var plainte = await _context.Plaintes.FindAsync(id);
            if (plainte == null)
            {
                return NotFound();
            }

            _context.Plaintes.Remove(plainte);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlainteExists(int id)
        {
            return _context.Plaintes.Any(e => e.Id == id);
        }
    }
}
