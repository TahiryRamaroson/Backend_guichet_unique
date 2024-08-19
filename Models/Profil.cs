using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Profil
{
    public int Id { get; set; }

    public string? Nom { get; set; }

    public string? Description { get; set; }
	[JsonIgnore]
	public virtual ICollection<Utilisateur> Utilisateurs { get; set; } = new List<Utilisateur>();
}
