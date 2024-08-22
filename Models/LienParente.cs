using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class LienParente
{
    public int IdTypeLien { get; set; }

    public int IdEnfant { get; set; }

    public int IdParent { get; set; }

    public virtual Individu IdEnfantNavigation { get; set; } = null!;

    public virtual Individu IdParentNavigation { get; set; } = null!;

    public virtual TypeLien IdTypeLienNavigation { get; set; } = null!;
}
