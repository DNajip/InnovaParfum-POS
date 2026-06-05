using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Configuracion
{
    public int IdConfig { get; set; }

    public string Clave { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool SoloAdmin { get; set; }

    public DateTime UltimaModificacion { get; set; }

    public int? ModificadoPor { get; set; }

    public virtual Usuario? ModificadoPorNavigation { get; set; }
}

