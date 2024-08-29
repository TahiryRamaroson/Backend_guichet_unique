using AutoMapper;
using Backend_guichet_unique.Models;
using Backend_guichet_unique.Models.DTO;

namespace Backend_guichet_unique.Services
{
	public class DistrictMapper : Profile
	{
		public DistrictMapper()
		{
			CreateMap<District, DistrictDTO>()
				.ForMember(dest => dest.Region, opt => opt.MapFrom(src => src.IdRegionNavigation));

			CreateMap<District, DistrictFormDTO>();

			CreateMap<DistrictFormDTO, District>()
			.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
			.ForMember(dest => dest.IdRegionNavigation, opt => opt.Ignore());
		}
	}
}
