using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Movimiento
{
    public int IdMovimiento { get; set; }

    public int IdProducto { get; set; }

    public int IdTipoMov { get; set; }

    public int Cantidad { get; set; }

    public int StockAntes { get; set; }

    public int StockDespues { get; set; }

    public int? IdReferencia { get; set; }

    public string? TablaReferencia { get; set; }

    public string? Observacion { get; set; }

    public DateTime FechaMov { get; set; }

    public int RegistradoPor { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual TipoMovimientoInv IdTipoMovNavigation { get; set; } = null!;

    public virtual Usuario RegistradoPorNavigation { get; set; } = null!;
}

