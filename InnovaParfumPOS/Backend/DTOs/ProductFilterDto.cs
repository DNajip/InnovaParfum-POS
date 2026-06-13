namespace InnovaParfumPOS.Backend.DTOs;

public class ProductFilterDto
{
    public string? SearchTerm { get; set; }
    
    // Filtros por Categoria
    public int? IdCategoria { get; set; }
    
    // Estado de Actividad: "activos", "inactivos", "todos"
    public string ActivityStatus { get; set; } = "activos";

    // Estado de Stock: "todos", "con_stock", "stock_bajo", "sin_stock"
    public string StockStatus { get; set; } = "todos";

    // Vencimiento
    public int? ExpireInMonths { get; set; }
    public bool ShowExpiredOnly { get; set; }

    // Atributos dinámicos
    public string? Marca { get; set; }
    public int? IdGenero { get; set; }
    public int? IdOrigen { get; set; }
    public decimal? Ml { get; set; }
}

