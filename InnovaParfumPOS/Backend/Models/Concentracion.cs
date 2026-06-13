using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Concentracion
{
    public int IdConcentracion { get; set; }
    public string Nombre { get; set; } = null!;
    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
