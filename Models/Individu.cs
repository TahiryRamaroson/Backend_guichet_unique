using System;
using System.Collections.Generic;

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

    public string IsChef { get; set; } = null!;

    public int IdMenage { get; set; }

    public virtual ICollection<Dece> Deces { get; set; } = new List<Dece>();

    public virtual ICollection<Grossesse> Grossesses { get; set; } = new List<Grossesse>();

    public virtual Menage IdMenageNavigation { get; set; } = null!;

    public virtual ICollection<LienParente> LienParenteIdEnfantNavigations { get; set; } = new List<LienParente>();

    public virtual ICollection<LienParente> LienParenteIdParentNavigations { get; set; } = new List<LienParente>();

    public virtual ICollection<MigrationEntrante> MigrationEntrantes { get; set; } = new List<MigrationEntrante>();

    public virtual ICollection<MigrationSortante> MigrationSortantes { get; set; } = new List<MigrationSortante>();

    public virtual ICollection<Naissance> NaissanceIdMereNavigations { get; set; } = new List<Naissance>();

    public virtual ICollection<Naissance> NaissanceIdPereNavigations { get; set; } = new List<Naissance>();

    public virtual ICollection<Plainte> Plaintes { get; set; } = new List<Plainte>();
}
