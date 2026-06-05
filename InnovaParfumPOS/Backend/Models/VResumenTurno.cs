using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VResumenTurno
{
    public int IdTurno { get; set; }

    public string? Cajero { get; set; }

    public DateTime FechaApertura { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal MontoInicialNio { get; set; }

    public decimal TotalVentasNio { get; set; }

    public decimal TotalEfectivoNio { get; set; }

    public decimal TotalEfectivoUsd { get; set; }

    public decimal? MontoContadoNio { get; set; }

    public decimal? DiferenciaNio { get; set; }

    public string? EstadoCuadre { get; set; }

    public string Estado { get; set; } = null!;
}

