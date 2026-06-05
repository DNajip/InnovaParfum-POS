using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Pago
{
    public int IdPago { get; set; }

    public int IdVenta { get; set; }

    public int IdMetodoPago { get; set; }

    public decimal MontoPagado { get; set; }

    public decimal? TasaAplicada { get; set; }

    public decimal MontoEnNio { get; set; }

    public decimal? MontoRecibido { get; set; }

    public decimal? VueltoNio { get; set; }

    public string? CodReferencia { get; set; }

    public DateTime FechaPago { get; set; }

    public virtual MetodosPago IdMetodoPagoNavigation { get; set; } = null!;

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}

