using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class MovimientoVario
{
    public int IdMovimiento { get; set; }

    public int IdTurno { get; set; }

    public string Tipo { get; set; } = "INGRESO";

    public int IdMoneda { get; set; }

    public decimal Monto { get; set; }

    public string Concepto { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public int IdUsuario { get; set; }

    public virtual Moneda IdMonedaNavigation { get; set; } = null!;

    public virtual Turno IdTurnoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

