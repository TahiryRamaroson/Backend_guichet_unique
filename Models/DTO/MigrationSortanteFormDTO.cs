namespace Backend_guichet_unique.Models.DTO
{
	public class MigrationSortanteFormDTO
	{
		public DateOnly DateDepart { get; set; }

		public int Destination { get; set; }

		public int StatutDepart { get; set; }

		public double? DureeAbsence { get; set; }

		public int? NouveauMenage { get; set; }

		public string? Adresse { get; set; }

		public IFormFile PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public int IdMotifMigration { get; set; }

		public int IdIndividu { get; set; }

		public int? IdFokontanyDestination { get; set; }

		public int IdIntervenant { get; set; }
	}
}
