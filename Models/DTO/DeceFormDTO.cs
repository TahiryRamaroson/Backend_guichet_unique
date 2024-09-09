namespace Backend_guichet_unique.Models.DTO
{
	public class DeceFormDTO
	{
		public int AgeDefunt { get; set; }

		public DateOnly DateDeces { get; set; }

		public IFormFile PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public int IdCauseDeces { get; set; }

		public int IdDefunt { get; set; }

		public int IdIntervenant { get; set; }
	}
}
