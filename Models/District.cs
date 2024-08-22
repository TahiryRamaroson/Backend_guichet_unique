using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class District
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public int IdRegion { get; set; }

    public virtual ICollection<Commune> Communes { get; set; } = new List<Commune>();

    public virtual Region IdRegionNavigation { get; set; } = null!;
}
