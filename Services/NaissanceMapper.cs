using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class NaissanceMapper : Profile
	{
		public NaissanceMapper()
		{
			CreateMap<Naissance, NaissanceFormDTO>();

			CreateMap<NaissanceFormDTO, Naissance>()
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
				.ForMember(dest => dest.IdFokontanyNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdIntervenantNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdResponsableNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdPereNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdMereNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdMenageNavigation, opt => opt.Ignore());

			CreateMap<Naissance, NaissanceDTO>()
				.ForMember(dest => dest.Menage, opt => opt.MapFrom(src => src.IdMenageNavigation))
				.ForMember(dest => dest.LieuNaissance, opt => opt.MapFrom(src => src.IdFokontanyNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation))
				.ForMember(dest => dest.Pere, opt => opt.MapFrom(src => src.IdPereNavigation))
				.ForMember(dest => dest.Mere, opt => opt.MapFrom(src => src.IdMereNavigation));
		}
	}
}
