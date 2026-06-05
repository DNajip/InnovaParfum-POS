using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class TipoIdentificacion
{
    public int IdTipo { get; set; }

    public string DescTipo { get; set; } = null!;

    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual ICollection<Persona> Personas { get; set; } = new List<Persona>();
}

