using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Backend_guichet_unique.Models;

public partial class GuichetUniqueContext : DbContext
{
    public GuichetUniqueContext()
    {
    }

    public GuichetUniqueContext(DbContextOptions<GuichetUniqueContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Action> Actions { get; set; }

    public virtual DbSet<AntecedentMedicaux> AntecedentMedicauxes { get; set; }

    public virtual DbSet<CategoriePlainte> CategoriePlaintes { get; set; }

    public virtual DbSet<CauseDece> CauseDeces { get; set; }

    public virtual DbSet<Commune> Communes { get; set; }

    public virtual DbSet<Dece> Deces { get; set; }

    public virtual DbSet<District> Districts { get; set; }

    public virtual DbSet<Fokontany> Fokontanies { get; set; }

    public virtual DbSet<Grossesse> Grossesses { get; set; }

    public virtual DbSet<HistoriqueActionPlainte> HistoriqueActionPlaintes { get; set; }

    public virtual DbSet<HistoriqueApplication> HistoriqueApplications { get; set; }

    public virtual DbSet<Individu> Individus { get; set; }

    public virtual DbSet<LienParente> LienParentes { get; set; }

    public virtual DbSet<Menage> Menages { get; set; }

    public virtual DbSet<MigrationEntrante> MigrationEntrantes { get; set; }

    public virtual DbSet<MigrationSortante> MigrationSortantes { get; set; }

    public virtual DbSet<MotifMigration> MotifMigrations { get; set; }

    public virtual DbSet<Naissance> Naissances { get; set; }

    public virtual DbSet<Plainte> Plaintes { get; set; }

    public virtual DbSet<Profil> Profils { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<TypeLien> TypeLiens { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Database=guichet_unique;Username=postgres;Password=Tahiry1849;Encoding=UTF8");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Action>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("action_pkey");

            entity.ToTable("action");

            entity.HasIndex(e => e.Nom, "action_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<AntecedentMedicaux>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("antecedent_medicaux_pkey");

            entity.ToTable("antecedent_medicaux");

            entity.HasIndex(e => e.Nom, "antecedent_medicaux_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<CategoriePlainte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categorie_plainte_pkey");

            entity.ToTable("categorie_plainte");

            entity.HasIndex(e => e.Nom, "categorie_plainte_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<CauseDece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cause_deces_pkey");

            entity.ToTable("cause_deces");

            entity.HasIndex(e => e.Nom, "cause_deces_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<Commune>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("commune_pkey");

            entity.ToTable("commune");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdDistrict).HasColumnName("id_district");
            entity.Property(e => e.Nom)
                .HasMaxLength(100)
                .HasColumnName("nom");

            entity.HasOne(d => d.IdDistrictNavigation).WithMany(p => p.Communes)
                .HasForeignKey(d => d.IdDistrict)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("commune_id_district_fkey");
        });

        modelBuilder.Entity<Dece>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("deces_pkey");

            entity.ToTable("deces");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgeDefunt).HasColumnName("age_defunt");
            entity.Property(e => e.DateDeces).HasColumnName("date_deces");
            entity.Property(e => e.IdCauseDeces).HasColumnName("id_cause_deces");
            entity.Property(e => e.IdDefunt).HasColumnName("id_defunt");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.PieceJustificative).HasColumnName("piece_justificative");

            entity.HasOne(d => d.IdCauseDecesNavigation).WithMany(p => p.Deces)
                .HasForeignKey(d => d.IdCauseDeces)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deces_id_cause_deces_fkey");

            entity.HasOne(d => d.IdDefuntNavigation).WithMany(p => p.Deces)
                .HasForeignKey(d => d.IdDefunt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deces_id_defunt_fkey");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.DeceIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deces_id_intervenant_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.DeceIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("deces_id_responsable_fkey");
        });

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("district_pkey");

            entity.ToTable("district");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdRegion).HasColumnName("id_region");
            entity.Property(e => e.Nom)
                .HasMaxLength(100)
                .HasColumnName("nom");

            entity.HasOne(d => d.IdRegionNavigation).WithMany(p => p.Districts)
                .HasForeignKey(d => d.IdRegion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("district_id_region_fkey");
        });

        modelBuilder.Entity<Fokontany>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fokontany_pkey");

            entity.ToTable("fokontany");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdCommune).HasColumnName("id_commune");
            entity.Property(e => e.Nom)
                .HasMaxLength(100)
                .HasColumnName("nom");

            entity.HasOne(d => d.IdCommuneNavigation).WithMany(p => p.Fokontanies)
                .HasForeignKey(d => d.IdCommune)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fokontany_id_commune_fkey");
        });

        modelBuilder.Entity<Grossesse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("grossesse_pkey");

            entity.ToTable("grossesse");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AgeMere).HasColumnName("age_mere");
            entity.Property(e => e.DateAccouchement).HasColumnName("date_accouchement");
            entity.Property(e => e.DerniereRegle).HasColumnName("derniere_regle");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdMere).HasColumnName("id_mere");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.PieceJustificative).HasColumnName("piece_justificative");
            entity.Property(e => e.RisqueComplication).HasColumnName("risque_complication");
            entity.Property(e => e.Statut).HasColumnName("statut");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.GrossesseIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("grossesse_id_intervenant_fkey");

            entity.HasOne(d => d.IdMereNavigation).WithMany(p => p.Grossesses)
                .HasForeignKey(d => d.IdMere)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("grossesse_id_mere_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.GrossesseIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("grossesse_id_responsable_fkey");

            entity.HasMany(d => d.IdAntecedentMedicauxes).WithMany(p => p.IdGrossesses)
                .UsingEntity<Dictionary<string, object>>(
                    "GrossesseAntecedantMedicaux",
                    r => r.HasOne<AntecedentMedicaux>().WithMany()
                        .HasForeignKey("IdAntecedentMedicaux")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("grossesse_antecedant_medicaux_id_antecedent_medicaux_fkey"),
                    l => l.HasOne<Grossesse>().WithMany()
                        .HasForeignKey("IdGrossesse")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("grossesse_antecedant_medicaux_id_grossesse_fkey"),
                    j =>
                    {
                        j.HasKey("IdGrossesse", "IdAntecedentMedicaux").HasName("grossesse_antecedant_medicaux_pkey");
                        j.ToTable("grossesse_antecedant_medicaux");
                        j.IndexerProperty<int>("IdGrossesse").HasColumnName("id_grossesse");
                        j.IndexerProperty<int>("IdAntecedentMedicaux").HasColumnName("id_antecedent_medicaux");
                    });
        });

        modelBuilder.Entity<HistoriqueActionPlainte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("historique_action_plainte_pkey");

            entity.ToTable("historique_action_plainte");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateVisite).HasColumnName("date_visite");
            entity.Property(e => e.IdPlainte).HasColumnName("id_plainte");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");

            entity.HasOne(d => d.IdPlainteNavigation).WithMany(p => p.HistoriqueActionPlaintes)
                .HasForeignKey(d => d.IdPlainte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("historique_action_plainte_id_plainte_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.HistoriqueActionPlaintes)
                .HasForeignKey(d => d.IdResponsable)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("historique_action_plainte_id_responsable_fkey");

            entity.HasMany(d => d.IdActions).WithMany(p => p.IdHistoriqueActionPlaintes)
                .UsingEntity<Dictionary<string, object>>(
                    "ActionPlainte",
                    r => r.HasOne<Action>().WithMany()
                        .HasForeignKey("IdAction")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("action_plainte_id_action_fkey"),
                    l => l.HasOne<HistoriqueActionPlainte>().WithMany()
                        .HasForeignKey("IdHistoriqueActionPlainte")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("action_plainte_id_historique_action_plainte_fkey"),
                    j =>
                    {
                        j.HasKey("IdHistoriqueActionPlainte", "IdAction").HasName("action_plainte_pkey");
                        j.ToTable("action_plainte");
                        j.IndexerProperty<int>("IdHistoriqueActionPlainte").HasColumnName("id_historique_action_plainte");
                        j.IndexerProperty<int>("IdAction").HasColumnName("id_action");
                    });
        });

        modelBuilder.Entity<HistoriqueApplication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("historique_application_pkey");

            entity.ToTable("historique_application");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(50)
                .HasColumnName("action");
            entity.Property(e => e.Composant)
                .HasMaxLength(50)
                .HasColumnName("composant");
            entity.Property(e => e.DateAction)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("date_action");
            entity.Property(e => e.IdUtilisateur).HasColumnName("id_utilisateur");
            entity.Property(e => e.UrlAction).HasColumnName("url_action");

            entity.HasOne(d => d.IdUtilisateurNavigation).WithMany(p => p.HistoriqueApplications)
                .HasForeignKey(d => d.IdUtilisateur)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("historique_application_id_utilisateur_fkey");
        });

        modelBuilder.Entity<Individu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("individu_pkey");

            entity.ToTable("individu");

            entity.HasIndex(e => e.Cin, "individu_cin_key").IsUnique();

            entity.HasIndex(e => e.NumActeNaissance, "individu_num_acte_naissance_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cin)
                .HasMaxLength(50)
                .HasColumnName("cin");
            entity.Property(e => e.DateNaissance).HasColumnName("date_naissance");
            entity.Property(e => e.IdMenage).HasColumnName("id_menage");
            entity.Property(e => e.IsChef).HasColumnName("is_chef");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
            entity.Property(e => e.NumActeNaissance)
                .HasMaxLength(50)
                .HasColumnName("num_acte_naissance");
            entity.Property(e => e.Prenom)
                .HasMaxLength(50)
                .HasColumnName("prenom");
            entity.Property(e => e.Sexe).HasColumnName("sexe");

            entity.HasOne(d => d.IdMenageNavigation).WithMany(p => p.Individus)
                .HasForeignKey(d => d.IdMenage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("individu_id_menage_fkey");
        });

        modelBuilder.Entity<LienParente>(entity =>
        {
            entity.HasKey(e => new { e.IdTypeLien, e.IdEnfant, e.IdParent }).HasName("lien_parente_pkey");

            entity.ToTable("lien_parente");

            entity.Property(e => e.IdTypeLien).HasColumnName("id_type_lien");
            entity.Property(e => e.IdEnfant).HasColumnName("id_enfant");
            entity.Property(e => e.IdParent).HasColumnName("id_parent");

            entity.HasOne(d => d.IdEnfantNavigation).WithMany(p => p.LienParenteIdEnfantNavigations)
                .HasForeignKey(d => d.IdEnfant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lien_parente_id_enfant_fkey");

            entity.HasOne(d => d.IdParentNavigation).WithMany(p => p.LienParenteIdParentNavigations)
                .HasForeignKey(d => d.IdParent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lien_parente_id_parent_fkey");

            entity.HasOne(d => d.IdTypeLienNavigation).WithMany(p => p.LienParentes)
                .HasForeignKey(d => d.IdTypeLien)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lien_parente_id_type_lien_fkey");
        });

        modelBuilder.Entity<Menage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("menage_pkey");

            entity.ToTable("menage");

            entity.HasIndex(e => e.Adresse, "menage_adresse_key").IsUnique();

            entity.HasIndex(e => e.NumeroMenage, "menage_numero_menage_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Adresse)
                .HasMaxLength(50)
                .HasColumnName("adresse");
            entity.Property(e => e.IdFokontany).HasColumnName("id_fokontany");
            entity.Property(e => e.NumeroMenage)
                .HasMaxLength(50)
                .HasColumnName("numero_menage");

            entity.HasOne(d => d.IdFokontanyNavigation).WithMany(p => p.Menages)
                .HasForeignKey(d => d.IdFokontany)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("menage_id_fokontany_fkey");
        });

        modelBuilder.Entity<MigrationEntrante>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("migration_entrante_pkey");

            entity.ToTable("migration_entrante");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateArrivee).HasColumnName("date_arrivee");
            entity.Property(e => e.DateRentree).HasColumnName("date_rentree");
            entity.Property(e => e.IdAncienMenage).HasColumnName("id_ancien_menage");
            entity.Property(e => e.IdIndividu).HasColumnName("id_individu");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdMotifMigration).HasColumnName("id_motif_migration");
            entity.Property(e => e.IdNouveauMenage).HasColumnName("id_nouveau_menage");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.PieceJustificative).HasColumnName("piece_justificative");
            entity.Property(e => e.Statut).HasColumnName("statut");
            entity.Property(e => e.StatutResidence)
                .HasMaxLength(50)
                .HasColumnName("statut_residence");

            entity.HasOne(d => d.IdAncienMenageNavigation).WithMany(p => p.MigrationEntranteIdAncienMenageNavigations)
                .HasForeignKey(d => d.IdAncienMenage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_entrante_id_ancien_menage_fkey");

            entity.HasOne(d => d.IdIndividuNavigation).WithMany(p => p.MigrationEntrantes)
                .HasForeignKey(d => d.IdIndividu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_entrante_id_individu_fkey");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.MigrationEntranteIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_entrante_id_intervenant_fkey");

            entity.HasOne(d => d.IdMotifMigrationNavigation).WithMany(p => p.MigrationEntrantes)
                .HasForeignKey(d => d.IdMotifMigration)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_entrante_id_motif_migration_fkey");

            entity.HasOne(d => d.IdNouveauMenageNavigation).WithMany(p => p.MigrationEntranteIdNouveauMenageNavigations)
                .HasForeignKey(d => d.IdNouveauMenage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_entrante_id_nouveau_menage_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.MigrationEntranteIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("migration_entrante_id_responsable_fkey");
        });

        modelBuilder.Entity<MigrationSortante>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("migration_sortante_pkey");

            entity.ToTable("migration_sortante");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Adresse)
                .HasMaxLength(50)
                .HasColumnName("adresse");
            entity.Property(e => e.DateDepart).HasColumnName("date_depart");
            entity.Property(e => e.Destination).HasColumnName("destination");
            entity.Property(e => e.DureeAbsence)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("duree_absence");
            entity.Property(e => e.IdFokontanyDestination).HasColumnName("id_fokontany_destination");
            entity.Property(e => e.IdIndividu).HasColumnName("id_individu");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdMotifMigration).HasColumnName("id_motif_migration");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.NouveauMenage).HasColumnName("nouveau_menage");
            entity.Property(e => e.PieceJustificative).HasColumnName("piece_justificative");
            entity.Property(e => e.Statut).HasColumnName("statut");
            entity.Property(e => e.StatutDepart).HasColumnName("statut_depart");

            entity.HasOne(d => d.IdFokontanyDestinationNavigation).WithMany(p => p.MigrationSortantes)
                .HasForeignKey(d => d.IdFokontanyDestination)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_sortante_id_fokontany_destination_fkey");

            entity.HasOne(d => d.IdIndividuNavigation).WithMany(p => p.MigrationSortantes)
                .HasForeignKey(d => d.IdIndividu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_sortante_id_individu_fkey");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.MigrationSortanteIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_sortante_id_intervenant_fkey");

            entity.HasOne(d => d.IdMotifMigrationNavigation).WithMany(p => p.MigrationSortantes)
                .HasForeignKey(d => d.IdMotifMigration)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("migration_sortante_id_motif_migration_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.MigrationSortanteIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("migration_sortante_id_responsable_fkey");
        });

        modelBuilder.Entity<MotifMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("motif_migration_pkey");

            entity.ToTable("motif_migration");

            entity.HasIndex(e => e.Nom, "motif_migration_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(100)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<Naissance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("naissance_pkey");

            entity.ToTable("naissance");

            entity.HasIndex(e => e.NumActeNaissance, "naissance_num_acte_naissance_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateNaissance).HasColumnName("date_naissance");
            entity.Property(e => e.IdFokontany).HasColumnName("id_fokontany");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdMenage).HasColumnName("id_menage");
            entity.Property(e => e.IdMere).HasColumnName("id_mere");
            entity.Property(e => e.IdPere).HasColumnName("id_pere");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.NomNouveauNe)
                .HasMaxLength(50)
                .HasColumnName("nom_nouveau_ne");
            entity.Property(e => e.NumActeNaissance)
                .HasMaxLength(50)
                .HasColumnName("num_acte_naissance");
            entity.Property(e => e.PieceJustificative).HasColumnName("piece_justificative");
            entity.Property(e => e.PrenomNouveauNe)
                .HasMaxLength(50)
                .HasColumnName("prenom_nouveau_ne");
            entity.Property(e => e.Sexe).HasColumnName("sexe");
            entity.Property(e => e.Statut).HasColumnName("statut");

            entity.HasOne(d => d.IdFokontanyNavigation).WithMany(p => p.Naissances)
                .HasForeignKey(d => d.IdFokontany)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("naissance_id_fokontany_fkey");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.NaissanceIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("naissance_id_intervenant_fkey");

            entity.HasOne(d => d.IdMenageNavigation).WithMany(p => p.Naissances)
                .HasForeignKey(d => d.IdMenage)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("naissance_id_menage_fkey");

            entity.HasOne(d => d.IdMereNavigation).WithMany(p => p.NaissanceIdMereNavigations)
                .HasForeignKey(d => d.IdMere)
                .HasConstraintName("naissance_id_mere_fkey");

            entity.HasOne(d => d.IdPereNavigation).WithMany(p => p.NaissanceIdPereNavigations)
                .HasForeignKey(d => d.IdPere)
                .HasConstraintName("naissance_id_pere_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.NaissanceIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("naissance_id_responsable_fkey");
        });

        modelBuilder.Entity<Plainte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plainte_pkey");

            entity.ToTable("plainte");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateFait).HasColumnName("date_fait");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IdCategoriePlainte).HasColumnName("id_categorie_plainte");
            entity.Property(e => e.IdIntervenant).HasColumnName("id_intervenant");
            entity.Property(e => e.IdResponsable).HasColumnName("id_responsable");
            entity.Property(e => e.IdVictime).HasColumnName("id_victime");
            entity.Property(e => e.Statut).HasColumnName("statut");
            entity.Property(e => e.StatutTraitement).HasColumnName("statut_traitement");

            entity.HasOne(d => d.IdCategoriePlainteNavigation).WithMany(p => p.Plaintes)
                .HasForeignKey(d => d.IdCategoriePlainte)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("plainte_id_categorie_plainte_fkey");

            entity.HasOne(d => d.IdIntervenantNavigation).WithMany(p => p.PlainteIdIntervenantNavigations)
                .HasForeignKey(d => d.IdIntervenant)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("plainte_id_intervenant_fkey");

            entity.HasOne(d => d.IdResponsableNavigation).WithMany(p => p.PlainteIdResponsableNavigations)
                .HasForeignKey(d => d.IdResponsable)
                .HasConstraintName("plainte_id_responsable_fkey");

            entity.HasOne(d => d.IdVictimeNavigation).WithMany(p => p.Plaintes)
                .HasForeignKey(d => d.IdVictime)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("plainte_id_victime_fkey");
        });

        modelBuilder.Entity<Profil>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("profil_pkey");

            entity.ToTable("profil");

            entity.HasIndex(e => e.Nom, "profil_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("region_pkey");

            entity.ToTable("region");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(100)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<TypeLien>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("type_lien_pkey");

            entity.ToTable("type_lien");

            entity.HasIndex(e => e.Nom, "type_lien_nom_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("utilisateur_pkey");

            entity.ToTable("utilisateur");

            entity.HasIndex(e => e.Email, "utilisateur_email_key").IsUnique();

            entity.HasIndex(e => e.Matricule, "utilisateur_matricule_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Adresse)
                .HasMaxLength(50)
                .HasColumnName("adresse");
            entity.Property(e => e.Contact)
                .HasMaxLength(20)
                .HasColumnName("contact");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.IdProfil).HasColumnName("id_profil");
            entity.Property(e => e.Matricule)
                .HasMaxLength(20)
                .HasColumnName("matricule");
            entity.Property(e => e.MotDePasse)
                .HasMaxLength(64)
                .HasColumnName("mot_de_passe");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
            entity.Property(e => e.Prenom)
                .HasMaxLength(50)
                .HasColumnName("prenom");
            entity.Property(e => e.Statut).HasColumnName("statut");

            entity.HasOne(d => d.IdProfilNavigation).WithMany(p => p.Utilisateurs)
                .HasForeignKey(d => d.IdProfil)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilisateur_id_profil_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
