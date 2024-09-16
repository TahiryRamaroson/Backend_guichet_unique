using AutoMapper;
using Backend_guichet_unique.Models.DTO;
using Backend_guichet_unique.Models;

namespace Backend_guichet_unique.Services
{
	public class MigrationEntranteMapper : Profile
	{
		public MigrationEntranteMapper()
		{
			CreateMap<MigrationEntrante, MigrationEntranteFormDTO>();

			CreateMap<MigrationEntranteFormDTO, MigrationEntrante>()
				.ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignorer l'Id si vous ne voulez pas le mettre à jour
				.ForMember(dest => dest.IdIntervenantNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdResponsableNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdAncienMenageNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdNouveauMenageNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdMotifMigrationNavigation, opt => opt.Ignore())
				.ForMember(dest => dest.IdIndividuNavigation, opt => opt.Ignore());

			CreateMap<MigrationEntrante, MigrationEntranteDTO>()
				.ForMember(dest => dest.AncienMenage, opt => opt.MapFrom(src => src.IdAncienMenageNavigation))
				.ForMember(dest => dest.NouveauMenage, opt => opt.MapFrom(src => src.IdNouveauMenageNavigation))
				.ForMember(dest => dest.Individu, opt => opt.MapFrom(src => src.IdIndividuNavigation))
				.ForMember(dest => dest.Intervenant, opt => opt.MapFrom(src => src.IdIntervenantNavigation))
				.ForMember(dest => dest.MotifMigration, opt => opt.MapFrom(src => src.IdMotifMigrationNavigation))
				.ForMember(dest => dest.Responsable, opt => opt.MapFrom(src => src.IdResponsableNavigation));
		}
	}
}
