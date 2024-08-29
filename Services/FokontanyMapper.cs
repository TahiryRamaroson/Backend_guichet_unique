using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class FokontanyMapper : Profile
	{
		public FokontanyMapper() 
		{
			CreateMap<Fokontany, FokontanyDTO>()
				.ForMember(dest => dest.Commune, opt => opt.MapFrom(src => src.IdCommuneNavigation));

			CreateMap<Fokontany, FokontanyFormDTO>();

			CreateMap<FokontanyFormDTO, Fokontany>()
			.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
			.ForMember(dest => dest.IdCommuneNavigation, opt => opt.Ignore());
		}
	}
}
