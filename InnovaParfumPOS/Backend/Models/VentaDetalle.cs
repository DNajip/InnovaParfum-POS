using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VentaDetalle
{
    public int IdDetalle { get; set; }

    public int IdVenta { get; set; }

    public int IdProducto { get; set; }

    public string DescripcionSnap { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal PrecioUnitarioNio { get; set; }

    public decimal DescuentoLineaNio { get; set; }

    public decimal SubtotalNio { get; set; }

    public int? IdPeriodoGarantia { get; set; }

    public DateOnly? FechaVenceGarantia { get; set; }

    public virtual ICollection<Garantia> Garantia { get; set; } = new List<Garantia>();

    public virtual PeriodosGarantium? IdPeriodoGarantiaNavigation { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}

