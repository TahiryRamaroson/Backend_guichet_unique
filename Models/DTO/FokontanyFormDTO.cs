using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models.DTO
{
	public class FokontanyFormDTO
	{
		[JsonIgnore]
		public int Id { get; set; }

		public string Nom { get; set; } = null!;

		public int idCommune { get; set; }
	}
}
