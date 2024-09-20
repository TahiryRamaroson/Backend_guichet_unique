namespace Backend_guichet_unique.Models.Statistique
{
	public class CommuneDataString
	{
		public int Id { get; set; }
		public string Nom { get; set; }
		public List<FokontanyDataString> Fokontanies { get; set; } = new List<FokontanyDataString>();
	}
}
