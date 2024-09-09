using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class DecesMapper : Profile
	{
		public DecesMapper()
		{
			CreateMap<Dece, DeceFormDTO>();

			CreateMap<DeceFormDTO, Dece>()
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
				.ForMember(dest => dest.IdIntervenantNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdResponsableNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdDefuntNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdCauseDecesNavigation, opt => opt.Ignore());

			CreateMap<Dece, DeceDTO>()
				.ForMember(dest => dest.CauseDeces, opt => opt.MapFrom(src => src.IdCauseDecesNavigation))
				.ForMember(dest => dest.Defunt, opt => opt.MapFrom(src => src.IdDefuntNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation));

			CreateMap<Individu, DefuntDTO>()
				.ForMember(dest => dest.Menage, opt => opt.MapFrom(src => src.IdMenageNavigation));
		}
	}
}
