using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class TypeLien
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<LienParente> LienParentes { get; set; } = new List<LienParente>();
}
