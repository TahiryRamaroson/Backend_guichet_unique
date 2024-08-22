using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Plainte
{
    public int Id { get; set; }

    public string Description { get; set; } = null!;

    public DateOnly DateFait { get; set; }

    public int Statut { get; set; }

    public int StatutTraitement { get; set; }

    public int IdVictime { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public int IdCategoriePlainte { get; set; }

    public virtual ICollection<HistoriqueActionPlainte> HistoriqueActionPlaintes { get; set; } = new List<HistoriqueActionPlainte>();

    public virtual CategoriePlainte IdCategoriePlainteNavigation { get; set; } = null!;

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual Utilisateur? IdResponsableNavigation { get; set; }

    public virtual Individu IdVictimeNavigation { get; set; } = null!;
}
