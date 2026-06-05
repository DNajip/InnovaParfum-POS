using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Modulo
{
    public int IdModulo { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Icono { get; set; }

    public string Controller { get; set; } = null!;

    public int Orden { get; set; }

    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual ICollection<Usuario> IdUsuarios { get; set; } = new List<Usuario>();
}

