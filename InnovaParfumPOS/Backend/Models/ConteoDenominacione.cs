using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class ConteoDenominacione
{
    public int IdConteo { get; set; }

    public int IdTurno { get; set; }

    public string TipoConteo { get; set; } = null!;

    public int IdDenominacion { get; set; }

    public int Cantidad { get; set; }

    public virtual Denominacione IdDenominacionNavigation { get; set; } = null!;

    public virtual Turno IdTurnoNavigation { get; set; } = null!;
}

