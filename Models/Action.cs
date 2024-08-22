using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Action
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<HistoriqueActionPlainte> IdHistoriqueActionPlaintes { get; set; } = new List<HistoriqueActionPlainte>();
}
