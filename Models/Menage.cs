using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Menage
{
    public int Id { get; set; }

    public string NumeroMenage { get; set; } = null!;

    public string Adresse { get; set; } = null!;

    public int IdFokontany { get; set; }

    public virtual Fokontany IdFokontanyNavigation { get; set; } = null!;

    public virtual ICollection<Individu> Individus { get; set; } = new List<Individu>();
    [JsonIgnore]
    public virtual ICollection<MigrationEntrante> MigrationEntranteIdAncienMenageNavigations { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<MigrationEntrante> MigrationEntranteIdNouveauMenageNavigations { get; set; } = new List<MigrationEntrante>();
	[JsonIgnore]
	public virtual ICollection<Naissance> Naissances { get; set; } = new List<Naissance>();
}
