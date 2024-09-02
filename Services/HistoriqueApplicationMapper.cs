using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class HistoriqueApplicationMapper : Profile
	{
		public HistoriqueApplicationMapper()
		{
			CreateMap<HistoriqueApplication, HistoriqueApplicationDTO>()
				.ForMember(dest => dest.Utilisateur, opt => opt.MapFrom(src => src.IdUtilisateurNavigation));
		}
	}
}
