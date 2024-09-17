using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class PlainteMapper : Profile
	{
		public PlainteMapper() 
		{
			CreateMap<Plainte, PlainteDTO>()
				.ForMember(dest => dest.HistoriqueActionPlaintes, opt => opt.MapFrom(src => src.HistoriqueActionPlaintes))
				.ForMember(dest => dest.Victime, opt => opt.MapFrom(src => src.IdVictimeNavigation))
				.ForMember(dest => dest.FokontanyFait, opt => opt.MapFrom(src => src.IdFokontanyFaitNavigation))
				.ForMember(dest => dest.CategoriePlainte, opt => opt.MapFrom(src => src.IdCategoriePlainteNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation));
		}
	}
}
