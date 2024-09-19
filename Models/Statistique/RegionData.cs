namespace Backend_guichet_unique.Models.Statistique
{
	public class RegionData
	{
		public string Region { get; set; }
		public int Data { get; set; }
		public List<DistrictData> Districts { get; set; } = new List<DistrictData>();
	}
}
