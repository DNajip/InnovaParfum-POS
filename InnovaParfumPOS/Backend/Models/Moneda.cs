using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Moneda
{
    public int IdMoneda { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Simbolo { get; set; } = null!;

    public bool EsBase { get; set; }

    public virtual ICollection<Denominacione> Denominaciones { get; set; } = new List<Denominacione>();

    public virtual ICollection<MetodosPago> MetodosPagos { get; set; } = new List<MetodosPago>();

    public virtual ICollection<MovimientoVario> MovimientosVarios { get; set; } = new List<MovimientoVario>();
}

