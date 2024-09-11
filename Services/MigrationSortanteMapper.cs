using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class MigrationSortanteMapper : Profile
	{
		public MigrationSortanteMapper()
		{
			CreateMap<MigrationSortante, MigrationSortanteFormDTO>();

			CreateMap<MigrationSortanteFormDTO, MigrationSortante>()
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
				.ForMember(dest => dest.IdIntervenantNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdResponsableNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdFokontanyDestinationNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdMotifMigrationNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdIndividuNavigation, opt => opt.Ignore());

			CreateMap<MigrationSortante, MigrationSortanteDTO>()
				.ForMember(dest => dest.FokontanyDestination, opt => opt.MapFrom(src => src.IdFokontanyDestinationNavigation))
				.ForMember(dest => dest.Individu, opt => opt.MapFrom(src => src.IdIndividuNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.MotifMigration, opt => opt.MapFrom(src => src.IdMotifMigrationNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation));

			CreateMap<Individu, IndividuDTO>()
				.ForMember(dest => dest.Menage, opt => opt.MapFrom(src => src.IdMenageNavigation));
		}
	}
}
