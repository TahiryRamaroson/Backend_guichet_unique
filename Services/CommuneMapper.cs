using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class CommuneMapper : Profile
	{
		public CommuneMapper()
		{
			CreateMap<Commune, CommuneDTO>()
				.ForMember(dest => dest.District, opt => opt.MapFrom(src => src.IdDistrictNavigation));

			CreateMap<Commune, CommuneFormDTO>();

			CreateMap<CommuneFormDTO, Commune>()
			.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
			.ForMember(dest => dest.IdDistrictNavigation, opt => opt.Ignore());
		}
	}
}
