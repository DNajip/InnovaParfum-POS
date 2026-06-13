using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VStockCritico
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Genero { get; set; }

    public string? OrigenTipo { get; set; }

    public string? Concentracion { get; set; }

    public int StockActual { get; set; }

    public int StockMinimo { get; set; }

    public string EstadoStock { get; set; } = null!;

    public string Categoria { get; set; } = null!;
}


