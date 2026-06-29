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

    public decimal PrecioUnitarioBase { get; set; }

    public decimal DescuentoLineaBase { get; set; }

    public decimal SubtotalBase { get; set; }
    
    public decimal? CostoUnitarioNio { get; set; }

    public int? IdPeriodoGarantia { get; set; }

    public DateOnly? FechaVenceGarantia { get; set; }

    public bool Devuelto { get; set; }

    public virtual ICollection<Garantia> Garantia { get; set; } = new List<Garantia>();

    public virtual PeriodosGarantium? IdPeriodoGarantiaNavigation { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Venta IdVentaNavigation { get; set; } = null!;
}

