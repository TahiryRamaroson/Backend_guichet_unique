using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Fokontany
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;

    public int IdCommune { get; set; }

    public virtual Commune IdCommuneNavigation { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<Menage> Menages { get; set; } = new List<Menage>();
	[JsonIgnore]
	public virtual ICollection<MigrationSortante> MigrationSortantes { get; set; } = new List<MigrationSortante>();
	[JsonIgnore]
	public virtual ICollection<Naissance> Naissances { get; set; } = new List<Naissance>();
}
