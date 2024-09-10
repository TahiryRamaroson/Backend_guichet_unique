namespace Backend_guichet_unique.Models
{
	public class Complication
	{
		public int Id { get; set; }
		public int Min { get; set; }
		public int Max { get; set; }
	}

	public class GrossesseSettings
	{
		public List<Complication> Complication { get; set; }
	}
}
