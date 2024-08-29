namespace Backend_guichet_unique.Models.DTO
{
	public class FokontanyDTO
	{
		public int Id { get; set; }

		public string Nom { get; set; } = null!;

		public CommuneDTO Commune { get; set; } = null!;
	}
}
