using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;
using AutoMapper;

namespace Backend_guichet_unique.Services
{
	public class ProfilMapper : Profile
	{
		public ProfilMapper()
		{
			CreateMap<ProfilDTO, Profil>()
				.ForMember(dest => dest.Utilisateurs, opt => opt.Ignore());
		}
	}
}
