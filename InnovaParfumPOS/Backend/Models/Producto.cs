using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public int? IdOrigen { get; set; }

    public int? IdGenero { get; set; }

    public int? IdConcentracion { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(8,2)")]
    public decimal? Ml { get; set; }

        public DateTime? FechaVencimiento { get; set; }
    public int? IdCategoria { get; set; }
    public string TipoProducto { get; set; } = "PERFUME";
    public decimal? CostoProducto { get; set; }
    public decimal? CostoEnvio { get; set; }
    public decimal? PorcGananciaMayorista { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public decimal? PorcGananciaMinorista { get; set; }
    public decimal? PrecioMinorista { get; set; }
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

    public virtual Origen? IdOrigenNavigation { get; set; }

    public virtual Concentracion? IdConcentracionNavigation { get; set; }

    public virtual Genero? IdGeneroNavigation { get; set; }

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

    public virtual ICollection<ReclamosGarantium> ReclamosGarantia { get; set; } = new List<ReclamosGarantium>();

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}


