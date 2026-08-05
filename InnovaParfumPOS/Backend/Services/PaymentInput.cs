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
    public decimal MontoEnBase { get; set; } // Obligatorio para contabilidad en BD
    public decimal VueltoMostrado { get; set; } // Vuelto mostrado al cliente al momento de imprimir el ticket
    public decimal VueltoEnMonedaBase { get; set; } // Equivalente del vuelto en la moneda principal
    public string? Referencia { get; set; }
}


