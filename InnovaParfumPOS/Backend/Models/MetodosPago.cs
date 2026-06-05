using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class MetodosPago
{
    public int IdMetodo { get; set; }

    public string Nombre { get; set; } = null!;

    public bool AfectaCaja { get; set; }

    public int? IdMoneda { get; set; }

    public virtual Moneda? IdMonedaNavigation { get; set; }

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

