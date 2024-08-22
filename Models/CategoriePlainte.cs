using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class CategoriePlainte
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<Plainte> Plaintes { get; set; } = new List<Plainte>();
}
