using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models.DTO
{
	public class UtilisateurPutDTO
	{
		[JsonIgnore]
		public int Id { get; set; }

		public string? Nom { get; set; }

		public string? Prenom { get; set; }

		public string? Contact { get; set; }

		public string? Adresse { get; set; }

		public string? Email { get; set; }

		public string? MotDePasse { get; set; }

		public int? IdProfil { get; set; }

		public int? Statut { get; set; }
	}
}
