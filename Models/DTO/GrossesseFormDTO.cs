namespace Backend_guichet_unique.Models.DTO
{
	public class GrossesseFormDTO
	{
		public int AgeMere { get; set; }

		public IFormFile PieceJustificative { get; set; } = null!;

		public DateOnly DerniereRegle { get; set; }

		public DateOnly DateAccouchement { get; set; }

		public List<int>? AntecedentMedicaux { get; set; } = new List<int>();

		public double RisqueComplication { get; set; }

		public int Statut { get; set; }

		public int StatutGrossesse { get; set; }

		public int IdMere { get; set; }

		public int IdIntervenant { get; set; }
	}
}
