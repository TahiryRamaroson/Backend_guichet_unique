using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class GrossesseMapper : Profile
	{
		public GrossesseMapper()
		{
			CreateMap<Grossesse, GrossesseFormDTO>();

			CreateMap<GrossesseFormDTO, Grossesse>()
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
				.ForMember(dest => dest.IdIntervenantNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdResponsableNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdMereNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdAntecedentMedicauxes, opt => opt.Ignore());

			CreateMap<Grossesse, GrossesseDTO>()
				.ForMember(dest => dest.AntecedentMedicauxes, opt => opt.MapFrom(src => src.IdAntecedentMedicauxes))
				.ForMember(dest => dest.Mere, opt => opt.MapFrom(src => src.IdMereNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation));
		}
	}
}
