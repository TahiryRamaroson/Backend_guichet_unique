using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Backend_guichet_unique.Models;

public partial class AntecedentMedicaux
{
    public int Id { get; set; }

    public string Nom { get; set; } = null!;
    [JsonIgnore]
    public virtual ICollection<Grossesse> IdGrossesses { get; set; } = new List<Grossesse>();
}
