namespace Backend_guichet_unique.Models.DTO
{
	public class DeceDTO
	{
		public int Id { get; set; }

		public int AgeDefunt { get; set; }

		public DateOnly DateDeces { get; set; }

		public string PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public CauseDece CauseDeces { get; set; } = null!;

		public DefuntDTO Defunt { get; set; } = null!;

		public Utilisateur Intervenant { get; set; } = null!;

		public Utilisateur? Responsable { get; set; }
	}
}
