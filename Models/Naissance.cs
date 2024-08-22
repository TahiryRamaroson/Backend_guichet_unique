using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Naissance
{
    public int Id { get; set; }

    public string NomNouveauNe { get; set; } = null!;

    public string PrenomNouveauNe { get; set; } = null!;

    public DateOnly DateNaissance { get; set; }

    public string? NumActeNaissance { get; set; }

    public int Sexe { get; set; }

    public string PieceJustificative { get; set; } = null!;

    public int Statut { get; set; }

    public int IdFokontany { get; set; }

    public int IdMenage { get; set; }

    public int? IdPere { get; set; }

    public int? IdMere { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public virtual Fokontany IdFokontanyNavigation { get; set; } = null!;

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual Menage IdMenageNavigation { get; set; } = null!;

    public virtual Individu? IdMereNavigation { get; set; }

    public virtual Individu? IdPereNavigation { get; set; }

    public virtual Utilisateur? IdResponsableNavigation { get; set; }
}
