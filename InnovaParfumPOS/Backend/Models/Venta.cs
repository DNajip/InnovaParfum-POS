using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Venta
{
    public int IdVenta { get; set; }

    public string? NumeroFactura { get; set; }

    public int IdTurno { get; set; }

    public int IdUsuario { get; set; }

    public int? IdPersona { get; set; }

    public decimal TasaCambioUsd { get; set; }

    public decimal SubtotalNio { get; set; }

    public decimal DescuentoNio { get; set; }

    public decimal TotalNio { get; set; }

    public string? Observacion { get; set; }

    public DateTime FechaVenta { get; set; }

    public bool Anulada { get; set; }

    public int? IdUsuarioAnula { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual Persona? IdPersonaNavigation { get; set; }

    public virtual Turno IdTurnoNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioAnulaNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}

