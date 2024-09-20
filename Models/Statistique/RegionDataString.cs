namespace Backend_guichet_unique.Models.Statistique
{
	public class RegionDataString
	{
		public string Region { get; set; }
		public string Data { get; set; }
		public List<DistrictDataString> Districts { get; set; } = new List<DistrictDataString>();
	}
}
