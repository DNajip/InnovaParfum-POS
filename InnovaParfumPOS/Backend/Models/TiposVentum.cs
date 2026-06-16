using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class TiposVentum
{
    public int IdTipoVenta { get; set; }

    public string Descripcion { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
