namespace Backend_guichet_unique.Models
{
	public class DecesTrancheAge
	{
		public int Id { get; set; }
		public int? Min { get; set; }
		public int? Max { get; set; }
		public string Color { get; set; }
	}

	public class DecesTrancheAgeSettings
	{
		public List<DecesTrancheAge> TrancheAge { get; set; }
	}
}
