namespace Backend_guichet_unique.Models.DTO
{
	public class UtilisateurDTO
	{
		public int Id { get; set; }

		public string Matricule { get; set; } = null!;

		public string? Nom { get; set; }

		public string? Prenom { get; set; }

		public string? Contact { get; set; }

		public string? Adresse { get; set; }

		public string? Email { get; set; }

		public string? MotDePasse { get; set; }

		public Profil? Profil { get; set; }

		public int? Statut { get; set; }
	}
}
