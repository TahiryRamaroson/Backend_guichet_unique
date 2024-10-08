﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class HistoriqueActionPlainte
{
    public int Id { get; set; }

    public DateOnly DateVisite { get; set; }

    public int IdPlainte { get; set; }

    public int IdResponsable { get; set; }
    [JsonIgnore]
    public virtual Plainte IdPlainteNavigation { get; set; } = null!;

    public virtual Utilisateur IdResponsableNavigation { get; set; } = null!;

    public virtual ICollection<Action> IdActions { get; set; } = new List<Action>();
}
