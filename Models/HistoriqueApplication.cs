using System;
using System.Collections.Generic;

namespace Backend_guichet_unique.Models;

public partial class HistoriqueApplication
{
    public int Id { get; set; }

    public string? Action { get; set; }

    public string? Composant { get; set; }

    public string? UrlAction { get; set; }

    public DateTime? DateAction { get; set; }

    public int IdUtilisateur { get; set; }

    public virtual Utilisateur IdUtilisateurNavigation { get; set; } = null!;
}
