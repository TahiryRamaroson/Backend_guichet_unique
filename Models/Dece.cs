using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Dece
{
    public int Id { get; set; }

    public int AgeDefunt { get; set; }

    public DateOnly DateDeces { get; set; }

    public string PieceJustificative { get; set; } = null!;

    public int IdCauseDeces { get; set; }

    public int IdDefunt { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public int Statut { get; set; }

	public virtual CauseDece IdCauseDecesNavigation { get; set; } = null!;

    public virtual Individu IdDefuntNavigation { get; set; } = null!;

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual Utilisateur? IdResponsableNavigation { get; set; }
}
