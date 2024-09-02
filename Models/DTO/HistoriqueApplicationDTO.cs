namespace Backend_guichet_unique.Models.DTO
{
	public class HistoriqueApplicationDTO
	{
		public int Id { get; set; }

		public string? Action { get; set; }

		public string? Composant { get; set; }

		public string? UrlAction { get; set; }

		public DateTime? DateAction { get; set; }

		public UtilisateurDTO? Utilisateur { get; set; }
	}
}
