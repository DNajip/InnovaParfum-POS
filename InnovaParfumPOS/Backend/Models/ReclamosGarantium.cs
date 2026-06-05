using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class ReclamosGarantium
{
    public int IdReclamo { get; set; }

    public int IdGarantia { get; set; }

    public DateTime FechaReclamo { get; set; }

    public string DescripcionFalla { get; set; } = null!;

    public string? TipoResolucion { get; set; }

    public int? ProductoReemplazoId { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public string? NotasTecnico { get; set; }

    public int IdUsuario { get; set; }

    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual Garantia IdGarantiaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Producto? ProductoReemplazo { get; set; }
}

