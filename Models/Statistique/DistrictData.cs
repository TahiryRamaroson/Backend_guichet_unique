namespace Backend_guichet_unique.Models.Statistique
{
	public class DistrictData
	{
		public int Id { get; set; }
		public string Nom { get; set; }
		public int Data { get; set; }
		public List<CommuneData> Communes { get; set; } = new List<CommuneData>();
	}
}
