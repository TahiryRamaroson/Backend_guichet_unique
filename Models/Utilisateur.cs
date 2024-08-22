using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Utilisateur
{
    public int Id { get; set; }

    public string Matricule { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public string Contact { get; set; } = null!;

    public string Adresse { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MotDePasse { get; set; } = null!;

    public int IdProfil { get; set; }

    public int Statut { get; set; }
    [JsonIgnore]
    public virtual ICollection<Dece> DeceIdIntervenantNavigations { get; set; } = new List<Dece>();
	[JsonIgnore]
	public virtual ICollection<Dece> DeceIdResponsableNavigations { get; set; } = new List<Dece>();
	[JsonIgnore]
	public virtual ICollection<Grossesse> GrossesseIdIntervenantNavigations { get; set; } = new List<Grossesse>();
	[JsonIgnore]
	public virtual ICollection<Grossesse> GrossesseIdResponsableNavigations { get; set; } = new List<Grossesse>();
	[JsonIgnore]
	public virtual ICollection<HistoriqueActionPlainte> HistoriqueActionPlaintes { get; set; } = new List<HistoriqueActionPlainte>();
	[JsonIgnore]
	public virtual ICollection<HistoriqueApplication> HistoriqueApplications { get; set; } = new List<HistoriqueApplication>();

    public virtual Profil IdProfilNavigation { get; set; } = null!;
	[JsonIgnore]
	public virtual ICollection<MigrationEntrante> MigrationEntranteIdIntervenantNavigations { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<MigrationEntrante> MigrationEntranteIdResponsableNavigations { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<MigrationSortante> MigrationSortanteIdIntervenantNavigations { get; set; } = new List<MigrationSortante>();
	[JsonIgnore]
	public virtual ICollection<MigrationSortante> MigrationSortanteIdResponsableNavigations { get; set; } = new List<MigrationSortante>();
	[JsonIgnore]
	public virtual ICollection<Naissance> NaissanceIdIntervenantNavigations { get; set; } = new List<Naissance>();
	[JsonIgnore]
	public virtual ICollection<Naissance> NaissanceIdResponsableNavigations { get; set; } = new List<Naissance>();
	[JsonIgnore]
	public virtual ICollection<Plainte> PlainteIdIntervenantNavigations { get; set; } = new List<Plainte>();
	[JsonIgnore]
	public virtual ICollection<Plainte> PlainteIdResponsableNavigations { get; set; } = new List<Plainte>();
}
