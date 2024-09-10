namespace Backend_guichet_unique.Models.DTO
{
	public class GrossesseDTO
	{
		public int Id { get; set; }

		public int AgeMere { get; set; }

		public string PieceJustificative { get; set; } = null!;

		public DateOnly DerniereRegle { get; set; }

		public DateOnly DateAccouchement { get; set; }

		public double RisqueComplication { get; set; }

		public int Statut { get; set; }

		public int StatutGrossesse { get; set; }

		public Individu Mere { get; set; }

		public Utilisateur Intervenant { get; set; }

		public Utilisateur? Responsable { get; set; }

		public List<AntecedentMedicaux> AntecedentMedicauxes { get; set; } = new List<AntecedentMedicaux>();
	}
}
