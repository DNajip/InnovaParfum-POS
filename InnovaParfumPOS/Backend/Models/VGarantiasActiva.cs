using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VGarantiasActiva
{
    public int IdGarantia { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public int? DiasRestantes { get; set; }

    public int MesesGarantia { get; set; }

    public string EstadoGarantia { get; set; } = null!;

    public string Producto { get; set; } = null!;

    public string? Cliente { get; set; }

    public string? Telefono { get; set; }
}

