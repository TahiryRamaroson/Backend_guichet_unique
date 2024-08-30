using AutoMapper;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;

namespace Backend_guichet_unique.Services
{
	public class CrudMapper : Profile
	{
		public CrudMapper()
		{
			CreateMap<TypeLien, TypeLienFormDTO>();
			CreateMap<TypeLienFormDTO, TypeLien>();

			CreateMap<AntecedentMedicaux, AntecedentMedicauxFormDTO>();
			CreateMap<AntecedentMedicauxFormDTO, AntecedentMedicaux>();
		}
	}
}
