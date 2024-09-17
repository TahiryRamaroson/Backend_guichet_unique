namespace Backend_guichet_unique.Models.DTO
{
	public class PlainteDTO
	{
		public int Id { get; set; }

		public string Description { get; set; } = null!;

		public DateOnly DateFait { get; set; }

		public int Statut { get; set; }

		public int StatutTraitement { get; set; }

		public IndividuDTO Victime { get; set; }

		public Utilisateur Intervenant { get; set; }

		public Utilisateur? Responsable { get; set; }

		public CategoriePlainte CategoriePlainte { get; set; }

		public FokontanyDTO FokontanyFait { get; set; }

		public List<HistoriqueActionPlainte> HistoriqueActionPlaintes { get; set; } = new List<HistoriqueActionPlainte>();
	}
}
