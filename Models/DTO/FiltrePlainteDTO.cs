namespace Backend_guichet_unique.Models.DTO
{
	public class FiltrePlainteDTO
	{
		public string? NumeroMenage { get; set; }
		public DateOnly? DateFait { get; set; }
		public int? idCategoriePlainte { get; set; }
		public int? Statut { get; set; }
	}
}
