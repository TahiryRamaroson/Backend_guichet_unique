namespace Backend_guichet_unique.Models.DTO
{
	public class FiltreDeceDTO
	{
		public string? NumeroMenage { get; set; }
		public DateOnly? DateDeces { get; set; }
		public int? CauseDeces { get; set; }
		public int? Statut { get; set; }
	}
}
