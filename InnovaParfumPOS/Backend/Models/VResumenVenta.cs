using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VResumenVenta
{
    public int IdVenta { get; set; }

    public string? NumeroFactura { get; set; }

    public DateTime FechaVenta { get; set; }

    public string? Cliente { get; set; }

    public string? Cajero { get; set; }

    public decimal SubtotalBase { get; set; }

    public decimal DescuentoBase { get; set; }

    public decimal TotalBase { get; set; }

    public bool Anulada { get; set; }

    public DateTime AperturaTurno { get; set; }
}

