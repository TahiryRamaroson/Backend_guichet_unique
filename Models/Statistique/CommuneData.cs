namespace Backend_guichet_unique.Models.Statistique
{
	public class CommuneData
	{
		public int Id { get; set; }
		public string Nom { get; set; }
		public int Data { get; set; }
		public List<FokontanyData> Fokontanies { get; set; } = new List<FokontanyData>();
	}
}
