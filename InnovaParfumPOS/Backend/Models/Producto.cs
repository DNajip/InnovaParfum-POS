using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public string? OrigenTipo { get; set; }

    public string? Genero { get; set; }

    public string? Concentracion { get; set; }

    public decimal? Ml { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int? IdCategoria { get; set; }
    
    public string TipoProducto { get; set; } = "PERFUME";



    public decimal? PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaDesactivacion { get; set; }

    public bool Archivado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int? CreadoPor { get; set; }

    public string EstadoStock { get; set; } = null!;

    public virtual Usuario? CreadoPorNavigation { get; set; }

    public virtual ICollection<Garantia> Garantia { get; set; } = new List<Garantia>();

    public virtual Categoria? IdCategoriaNavigation { get; set; }

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

    public virtual ICollection<ReclamosGarantium> ReclamosGarantia { get; set; } = new List<ReclamosGarantium>();

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}

