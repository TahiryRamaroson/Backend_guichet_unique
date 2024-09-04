using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class MenageMapper : Profile
	{
		public MenageMapper()
		{
			CreateMap<Menage, MenageDTO>()
				.ForMember(dest => dest.Fokontany, opt => opt.MapFrom(src => src.IdFokontanyNavigation))
				.ForMember(dest => dest.Individu, opt => opt.MapFrom(src => src.Individus.FirstOrDefault(i => i.IsChef == 1)));
		}
	}
}
