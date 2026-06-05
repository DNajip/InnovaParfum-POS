using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class TipoMovimientoInv
{
    public int IdTipo { get; set; }

    public string Nombre { get; set; } = null!;

    public string Signo { get; set; } = null!;

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
}

