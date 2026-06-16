using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class CondicionesPago
{
    public int IdCondicion { get; set; }

    public string Descripcion { get; set; } = null!;

    public int DiasPlazo { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
