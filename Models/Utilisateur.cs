using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class Utilisateur
{
    public int Id { get; set; }

    public string Matricule { get; set; } = null!;

    public string? Nom { get; set; }

    public string? Prenom { get; set; }

    public string? Contact { get; set; }

    public string? Adresse { get; set; }

    public string? Email { get; set; }

    public string? MotDePasse { get; set; }

    public int? IdProfil { get; set; }

    public int? Statut { get; set; }

	[JsonIgnore]
	public virtual Profil? IdProfilNavigation { get; set; }
}
