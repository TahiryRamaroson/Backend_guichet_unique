using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Region
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;
	[JsonIgnore]
	public virtual ICollection<District> Districts { get; set; } = new List<District>();
}
