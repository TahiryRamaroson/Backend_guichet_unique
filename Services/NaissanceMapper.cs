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
		}
	}
}
