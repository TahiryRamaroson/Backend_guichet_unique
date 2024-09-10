namespace Backend_guichet_unique.Models.DTO
{
	public class FiltreGrossesseDTO
	{
		public string? NumeroMenage { get; set; }
		public DateOnly? Dpa { get; set; }
		public int? RisqueComplication { get; set; }
		public int? Statut { get; set; }
	}
}
