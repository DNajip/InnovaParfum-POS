namespace InnovaParfumPOS.Backend.DTOs;

public class InventoryStatsDto
{
    public int TotalProductos { get; set; }
    public int StockBajo { get; set; }      // CRITICO
    public int SinStock { get; set; }       // AGOTADO
    public decimal Valorizacion { get; set; } // SUM(PrecioVenta * StockActual)
}

