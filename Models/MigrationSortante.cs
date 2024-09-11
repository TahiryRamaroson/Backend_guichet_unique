using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class MigrationSortante
{
    public int Id { get; set; }

    public DateOnly DateDepart { get; set; }

    public int Destination { get; set; }

    public int StatutDepart { get; set; }

    public double? DureeAbsence { get; set; }

    public int? NouveauMenage { get; set; }

    public string? Adresse { get; set; }

    public string PieceJustificative { get; set; } = null!;

    public int Statut { get; set; }

    public int IdMotifMigration { get; set; }

    public int IdIndividu { get; set; }

    public int? IdFokontanyDestination { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public virtual Fokontany IdFokontanyDestinationNavigation { get; set; } = null!;

    public virtual Individu IdIndividuNavigation { get; set; } = null!;

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual MotifMigration IdMotifMigrationNavigation { get; set; } = null!;

    public virtual Utilisateur? IdResponsableNavigation { get; set; }
}
