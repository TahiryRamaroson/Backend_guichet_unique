using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class Region
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public virtual ICollection<District> Districts { get; set; } = new List<District>();
}
