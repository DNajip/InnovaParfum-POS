using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Turno
{
    public int IdTurno { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal MontoInicialBase { get; set; }

    public decimal MontoInicialUsd { get; set; }

    public decimal TotalVentasBase { get; set; }

    public decimal TotalVentasUsd { get; set; }

    public decimal TotalEfectivoBase { get; set; }

    public decimal TotalEfectivoUsd { get; set; }

    public decimal TotalTarjeta { get; set; }

    public decimal TotalTransferencia { get; set; }

    public decimal? MontoContadoBase { get; set; }

    public decimal? MontoContadoUsd { get; set; }

    public decimal? DiferenciaBase { get; set; }

    public decimal? DiferenciaUsd { get; set; }

    public string? EstadoCuadre { get; set; }

    public string? Observaciones { get; set; }

    public int IdEstado { get; set; }

    public virtual ICollection<ConteoDenominacione> ConteoDenominaciones { get; set; } = new List<ConteoDenominacione>();

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();

    public virtual ICollection<MovimientoVario> MovimientosVarios { get; set; } = new List<MovimientoVario>();
}

