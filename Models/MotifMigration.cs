using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class MotifMigration
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<MigrationEntrante> MigrationEntrantes { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<MigrationSortante> MigrationSortantes { get; set; } = new List<MigrationSortante>();
}
