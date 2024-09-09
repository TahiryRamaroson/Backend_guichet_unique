namespace Backend_guichet_unique.Models.DTO
{
	public class DefuntDTO
	{
		public int Id { get; set; }

		public string Nom { get; set; } = null!;

		public string Prenom { get; set; } = null!;

		public DateOnly DateNaissance { get; set; }

		public int Sexe { get; set; }

		public string? NumActeNaissance { get; set; }

		public string? Cin { get; set; }

		public int IsChef { get; set; }

		public MenageDTO Menage { get; set; }

		public int Statut { get; set; }
	}
}
