using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Denominacione
{
    public int IdDenominacion { get; set; }

    public int IdMoneda { get; set; }

    public decimal Valor { get; set; }

    public string Tipo { get; set; } = null!;

    public int Orden { get; set; }

    public virtual ICollection<ConteoDenominacione> ConteoDenominaciones { get; set; } = new List<ConteoDenominacione>();

    public virtual Moneda IdMonedaNavigation { get; set; } = null!;
}

