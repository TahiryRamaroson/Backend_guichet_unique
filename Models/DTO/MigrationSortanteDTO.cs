namespace Backend_guichet_unique.Models.DTO
{
	public class MigrationSortanteDTO
	{
		public int Id { get; set; }

		public DateOnly DateDepart { get; set; }

		public int Destination { get; set; }

		public int StatutDepart { get; set; }

		public double? DureeAbsence { get; set; }

		public int? NouveauMenage { get; set; }

		public string? Adresse { get; set; }

		public string PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public MotifMigration MotifMigration { get; set; }

		public IndividuDTO Individu { get; set; }

		public FokontanyDTO FokontanyDestination { get; set; }

		public Utilisateur Intervenant { get; set; }

		public Utilisateur? Responsable { get; set; }
	}
}
