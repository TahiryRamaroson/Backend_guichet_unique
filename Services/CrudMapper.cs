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

			CreateMap<CauseDece, CauseDeceFormDTO>();
			CreateMap<CauseDeceFormDTO, CauseDece>();

			CreateMap<CategoriePlainte, CategoriePlainteFormDTO>();
			CreateMap<CategoriePlainteFormDTO, CategoriePlainte>();

			CreateMap<Models.Action, ActionFormDTO>();
			CreateMap<ActionFormDTO, Models.Action>();
		}
	}
}
