namespace InnovaParfumPOS.Backend.DTOs;

public class ClienteStatsDto
{
    public int TotalClientes { get; set; }
    public int ConGarantiasActivas { get; set; }
    public int ConComprasRecientes { get; set; } // últimos 30 días
}

