using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class RegionMapper : Profile
	{
		public RegionMapper()
		{
			CreateMap<RegionDTO, Region>()
				.ForMember(dest => dest.Id, opt => opt.Ignore());
		}
	}
}
