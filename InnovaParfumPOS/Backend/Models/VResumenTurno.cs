using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VResumenTurno
{
    public int IdTurno { get; set; }

    public string? Cajero { get; set; }

    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal MontoInicialBase { get; set; }

    public decimal TotalVentasBase { get; set; }

    public decimal TotalEfectivoBase { get; set; }

    public decimal TotalEfectivoUsd { get; set; }

    public decimal? MontoContadoBase { get; set; }

    public decimal? DiferenciaBase { get; set; }

    public string? EstadoCuadre { get; set; }

    public string Estado { get; set; } = null!;
}

