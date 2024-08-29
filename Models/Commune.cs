using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Commune
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public int IdDistrict { get; set; }
    [JsonIgnore]
    public virtual ICollection<Fokontany> Fokontanies { get; set; } = new List<Fokontany>();

    public virtual District IdDistrictNavigation { get; set; } = null!;
}
