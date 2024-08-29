using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class District
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public int IdRegion { get; set; }
    [JsonIgnore]
    public virtual ICollection<Commune> Communes { get; set; } = new List<Commune>();

    public virtual Region IdRegionNavigation { get; set; } = null!;
}
