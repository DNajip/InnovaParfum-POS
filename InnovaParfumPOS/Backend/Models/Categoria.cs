using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Categoria
{
    public int IdCategoria { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int IdEstado { get; set; }

    public bool PedirMarca { get; set; } = true;
    public bool PedirGenero { get; set; } = true;
    public bool PedirVencimiento { get; set; } = true;
    public bool PedirConcentracion { get; set; } = true;
    public bool PedirOrigen { get; set; } = true;

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

