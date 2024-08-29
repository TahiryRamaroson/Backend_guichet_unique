namespace Backend_guichet_unique.Models.DTO
{
	public class CommuneDTO
	{
		public int Id { get; set; }

		public string Nom { get; set; } = null!;

		public DistrictDTO District { get; set; } = null!;
	}
}
