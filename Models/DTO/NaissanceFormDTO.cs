using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models.DTO
{
	public class NaissanceFormDTO
	{
		public string NomNouveauNe { get; set; } = null!;

		public string PrenomNouveauNe { get; set; } = null!;

		public DateOnly DateNaissance { get; set; }

		public string? NumActeNaissance { get; set; }

		public int Sexe { get; set; }

		public IFormFile PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public int IdFokontany { get; set; }

		public int IdMenage { get; set; }

		public int? IdPere { get; set; }

		public int? IdMere { get; set; }

		public int IdIntervenant { get; set; }

		public int? IdResponsable { get; set; }
	}
}
