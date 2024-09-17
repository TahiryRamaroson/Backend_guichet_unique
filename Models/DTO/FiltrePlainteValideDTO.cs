namespace Backend_guichet_unique.Models.DTO
{
	public class FiltrePlainteValideDTO
	{
		public string? NumeroMenage { get; set; }
		public DateOnly? DateFait { get; set; }
		public int? idCategoriePlainte { get; set; }
		public int? StatutTraitement { get; set; }
	}
}
