namespace Backend_guichet_unique.Models.Statistique
{
	public class NaissanceParFokontanyEtRegion
	{
		public string RegionNom { get; set; }
		public string DistrictNom { get; set; }
		public string CommuneNom { get; set; }
		public string FokontanyNom { get; set; }
		public int NombreNaissancesFokontany { get; set; }
		public int NombreNaissancesRegion { get; set; }
	}
}
