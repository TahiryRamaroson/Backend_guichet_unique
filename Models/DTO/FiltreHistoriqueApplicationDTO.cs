namespace Backend_guichet_unique.Models.DTO
{
	public class FiltreHistoriqueApplicationDTO
	{
		public string? text { get; set; }
		public string? action { get; set; }
		public string? composant { get; set; }
		public DateOnly? date { get; set; }
	}
}
