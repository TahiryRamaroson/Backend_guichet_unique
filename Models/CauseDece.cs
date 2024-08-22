using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class CauseDece
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<Dece> Deces { get; set; } = new List<Dece>();
}
