namespace Backend_guichet_unique.Models.DTO
{
	public class MigrationEntranteDTO
	{
		public int Id { get; set; }

		public DateOnly DateArrivee { get; set; }

		public int StatutResidence { get; set; }

		public DateOnly? DateRentree { get; set; }

		public string PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public IndividuDTO Individu { get; set; }

		public MenageDTO AncienMenage { get; set; }

		public MenageDTO NouveauMenage { get; set; }

		public MotifMigration MotifMigration { get; set; }

		public Utilisateur Intervenant { get; set; }

		public Utilisateur? Responsable { get; set; }
	}
}
