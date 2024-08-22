using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Grossesse
{
    public int Id { get; set; }

    public int AgeMere { get; set; }

    public string PieceJustificative { get; set; } = null!;

    public DateOnly DerniereRegle { get; set; }

    public DateOnly DateAccouchement { get; set; }

    public double RisqueComplication { get; set; }

    public int Statut { get; set; }

    public int IdMere { get; set; }

    public int IdIntervenant { get; set; }

    public int? IdResponsable { get; set; }

    public virtual Utilisateur IdIntervenantNavigation { get; set; } = null!;

    public virtual Individu IdMereNavigation { get; set; } = null!;

    public virtual Utilisateur? IdResponsableNavigation { get; set; }

    public virtual ICollection<AntecedentMedicaux> IdAntecedentMedicauxes { get; set; } = new List<AntecedentMedicaux>();
}
