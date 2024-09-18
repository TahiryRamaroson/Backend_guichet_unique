namespace Backend_guichet_unique.Models.DTO
{
	public class PlainteActionFormDTO
	{
		public DateOnly DateVisite { get; set; }
		public List<int> Actions { get; set; } = new List<int>();
	}
}
