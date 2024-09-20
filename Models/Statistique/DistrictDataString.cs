namespace Backend_guichet_unique.Models.Statistique
{
	public class DistrictDataString
	{
		public int Id { get; set; }
		public string Nom { get; set; }
		public List<CommuneDataString> Communes { get; set; } = new List<CommuneDataString>();
	}
}
