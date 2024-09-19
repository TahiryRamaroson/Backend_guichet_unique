namespace Backend_guichet_unique.Models
{
	public class GrossesseTrancheAge
	{
		public int Id { get; set; }
		public int? Min { get; set; }
		public int? Max { get; set; }
		public string Color { get; set; }
	}

	public class GrossesseTrancheAgeSettings
	{
		public List<GrossesseTrancheAge> TrancheAge { get; set; }
	}
}
