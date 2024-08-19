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

    public virtual DbSet<Profil> Profils { get; set; }

    public virtual DbSet<Utilisateur> Utilisateurs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profil>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("profil_pkey");

            entity.ToTable("profil");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
            entity.Property(e => e.Nom)
                .HasMaxLength(50)
                .HasColumnName("nom");
        });

        modelBuilder.Entity<Utilisateur>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("utilisateur_pkey");

            entity.ToTable("utilisateur");

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
                .HasConstraintName("utilisateur_id_profil_fkey");
        });
        modelBuilder.HasSequence("seq_matricule_utilisateur");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
