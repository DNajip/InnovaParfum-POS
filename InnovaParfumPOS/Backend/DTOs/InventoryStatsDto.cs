namespace InnovaParfumPOS.Backend.DTOs;

public class InventoryStatsDto
{
    public int TotalProductos { get; set; }
    public int TotalCantidades { get; set; }
    public int StockBajo { get; set; }      // CRITICO
    public int SinStock { get; set; }       // AGOTADO
    public decimal ValorizacionMayorista { get; set; } // SUM(PrecioMayorista * StockActual)
    public decimal ValorizacionMinorista { get; set; } // SUM(PrecioMinorista * StockActual)
}


