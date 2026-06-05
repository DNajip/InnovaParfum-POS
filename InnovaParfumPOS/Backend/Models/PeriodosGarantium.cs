using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class PeriodosGarantium
{
    public int IdPeriodo { get; set; }

    public string Descripcion { get; set; } = null!;

    public int Meses { get; set; }

    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual ICollection<VentaDetalle> VentaDetalles { get; set; } = new List<VentaDetalle>();
}

