using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Garantia
{
    public int IdGarantia { get; set; }

    public int IdDetalleVenta { get; set; }

    public int IdPersona { get; set; }

    public int IdProducto { get; set; }

    public int MesesGarantia { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string EstadoGarantia { get; set; } = null!;

    public virtual VentaDetalle IdDetalleVentaNavigation { get; set; } = null!;

    public virtual Persona IdPersonaNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual ICollection<ReclamosGarantium> ReclamosGarantia { get; set; } = new List<ReclamosGarantium>();
}

