namespace Backend_guichet_unique.Models.DTO
{
	public class DistrictDTO
	{
		public int Id { get; set; }

		public string Nom { get; set; } = null!;

		public Region Region { get; set; } = null!;
	}
}
