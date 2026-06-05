using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Genero
{
    public int IdGenero { get; set; }

    public string DescGenero { get; set; } = null!;

    public int IdEstado { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual ICollection<Persona> Personas { get; set; } = new List<Persona>();
}

