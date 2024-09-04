namespace Backend_guichet_unique.Models.DTO
{
	public class MenageDTO
	{
		public int Id { get; set; }

		public string NumeroMenage { get; set; } = null!;

		public string Adresse { get; set; } = null!;

		public FokontanyDTO Fokontany { get; set; }

		public Individu Individu { get; set; }
	}
}
