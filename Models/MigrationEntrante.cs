using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class MigrationEntrante
{
    public int Id { get; set; }

    public DateOnly DateArrivee { get; set; }

    public string StatutResidence { get; set; } = null!;

    public DateOnly? DateRentree { get; set; }

    public string PieceJustificative { get; set; } = null!;

    public int Statut { get; set; }

    public int IdIndividu { get; set; }

    public int IdAncienMenage { get; set; }

    public int IdNouveauMenage { get; set; }

    public int IdMotifMigration { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public virtual Menage IdAncienMenageNavigation { get; set; } = null!;

    public virtual Individu IdIndividuNavigation { get; set; } = null!;

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual MotifMigration IdMotifMigrationNavigation { get; set; } = null!;

    public virtual Menage IdNouveauMenageNavigation { get; set; } = null!;

    public virtual Utilisateur? IdResponsableNavigation { get; set; }
}
