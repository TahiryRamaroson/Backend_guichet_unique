using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class UtilisateurService : Profile
	{
		public UtilisateurService()
		{
			CreateMap<Utilisateur, UtilisateurDTO>()
				.ForMember(dest => dest.Profil, opt => opt.MapFrom(src => src.IdProfilNavigation));
		}
	}
}
