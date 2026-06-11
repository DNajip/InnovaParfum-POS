using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Services;

public class PaymentInput
{
    public int IdMetodoPago { get; set; }
    public string MetodoNombre { get; set; } = "";
    public decimal Monto { get; set; } // Monto en la moneda original
    public decimal TasaCambio { get; set; }
    public decimal MontoEnMonedaBase { get; set; } // Equivalente en la moneda principal de la tienda (NIO o USD)
    public decimal MontoEnNio { get; set; } // Obligatorio para contabilidad en BD
    public string? Referencia { get; set; }
}

