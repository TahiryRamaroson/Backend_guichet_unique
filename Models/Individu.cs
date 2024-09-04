using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Individu
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public DateOnly DateNaissance { get; set; }

    public int Sexe { get; set; }

    public string? NumActeNaissance { get; set; }

    public string? Cin { get; set; }

    public int IsChef { get; set; }

    public int IdMenage { get; set; }
    [JsonIgnore]
    public virtual ICollection<Dece> Deces { get; set; } = new List<Dece>();
	[JsonIgnore]
	public virtual ICollection<Grossesse> Grossesses { get; set; } = new List<Grossesse>();
	[JsonIgnore]
	public virtual Menage IdMenageNavigation { get; set; } = null!;
	[JsonIgnore]
	public virtual ICollection<LienParente> LienParenteIdEnfantNavigations { get; set; } = new List<LienParente>();
	[JsonIgnore]
	public virtual ICollection<LienParente> LienParenteIdParentNavigations { get; set; } = new List<LienParente>();
	[JsonIgnore]
	public virtual ICollection<MigrationEntrante> MigrationEntrantes { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<MigrationSortante> MigrationSortantes { get; set; } = new List<MigrationSortante>();
	[JsonIgnore]
	public virtual ICollection<Naissance> NaissanceIdMereNavigations { get; set; } = new List<Naissance>();
	[JsonIgnore]
	public virtual ICollection<Naissance> NaissanceIdPereNavigations { get; set; } = new List<Naissance>();
	[JsonIgnore]
	public virtual ICollection<Plainte> Plaintes { get; set; } = new List<Plainte>();
}
