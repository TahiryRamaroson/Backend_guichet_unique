namespace Backend_guichet_unique.Models.DTO
{
	public class MigrationEntranteFormDTO
	{
		public DateOnly DateArrivee { get; set; }

		public int StatutResidence { get; set; }

		public DateOnly? DateRentree { get; set; }

		public IFormFile PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public int IdIndividu { get; set; }

		public int IdAncienMenage { get; set; }

		public int IdNouveauMenage { get; set; }

		public int IdMotifMigration { get; set; }

		public int IdIntervenant { get; set; }
	}
}
