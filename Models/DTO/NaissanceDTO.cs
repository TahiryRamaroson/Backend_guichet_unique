namespace Backend_guichet_unique.Models.DTO
{
	public class NaissanceDTO
	{
		public int Id { get; set; }

		public string NomNouveauNe { get; set; } = null!;

		public string PrenomNouveauNe { get; set; } = null!;

		public DateOnly DateNaissance { get; set; }

		public string? NumActeNaissance { get; set; }

		public int Sexe { get; set; }

		public string PieceJustificative { get; set; } = null!;

		public int Statut { get; set; }

		public FokontanyDTO LieuNaissance { get; set; }

		public Menage Menage { get; set; }

		public Individu? Pere { get; set; }

		public Individu? Mere { get; set; }

		public Utilisateur Intervenant { get; set; }

		public Utilisateur? Responsable { get; set; }
	}
}
