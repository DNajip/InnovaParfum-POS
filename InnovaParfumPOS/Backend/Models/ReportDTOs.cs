using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public class DashboardStatsDTO
{
    public decimal VentasBrutas { get; set; }
    public decimal UtilidadNeta { get; set; }
    public int TotalFacturas { get; set; }
    public decimal TicketPromedio { get; set; }
    public int ClientesNuevos { get; set; }
    public int ProductosVendidos { get; set; }
    public int Anulaciones { get; set; }
    
    // Comparativas con periodo anterior
    public decimal PorcentajeVentas { get; set; }
    public decimal PorcentajeUtilidad { get; set; }
    public decimal PorcentajeFacturas { get; set; }
    public decimal PorcentajeTicket { get; set; }
    public decimal PorcentajeClientes { get; set; }
    public decimal PorcentajeProductos { get; set; }
    public decimal MargenUtilidadPorcentaje => VentasBrutas > 0 ? (UtilidadNeta / VentasBrutas) * 100 : 0;
}

public class TrendPointDTO
{
    public string Label { get; set; } = string.Empty;
    public decimal ValorNio { get; set; }
    public decimal ValorUsd { get; set; }
}

public class PaymentMethodStatDTO
{
    public string Metodo { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public double Porcentaje { get; set; }
}

public class TopProductoDTO
{
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal TotalVentas { get; set; }
}

public class ResumenDiarioDTO
{
    public DateTime Fecha { get; set; }
    public decimal VentasBrutas { get; set; }
    public decimal Devoluciones { get; set; }
    public decimal VentasNetas { get; set; }
    public int Facturas { get; set; }
    public decimal TicketPromedio { get; set; }
}

public class HourlySalesDTO
{
    public int DayOfWeek { get; set; } // 0=Sun, 1=Mon...
    public int Hour { get; set; }
    public decimal Total { get; set; }
    public int Intensity { get; set; } // 0-10 for heatmap
}

public class InventoryInsightDTO
{
    public List<VStockValorizadoDTO> StockCritico { get; set; } = new();
    public List<ProductNoMovementDTO> SinMovimiento { get; set; } = new();
    public decimal ValorTotalCosto { get; set; }
    public decimal ValorTotalVenta { get; set; }
}

public class VStockValorizadoDTO
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Marca { get; set; } = "";
    public string OrigenTipo { get; set; } = "";
    public string Concentracion { get; set; } = "";
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public string EstadoStock { get; set; } = "";
    public decimal CostoProducto { get; set; }
    public decimal PrecioMinorista { get; set; }
    public decimal ValorCostoTotal => StockActual * CostoProducto;
    public decimal ValorVentaTotal => StockActual * PrecioMinorista;
}

public class ProductNoMovementDTO
{
    public string Nombre { get; set; } = string.Empty;
    public DateTime? UltimaVenta { get; set; }
    public int DiasSinVenta { get; set; }
}

public class ClientInsightDTO
{
    public string Nombre { get; set; } = string.Empty;
    public int TotalCompras { get; set; }
    public decimal MontoTotal { get; set; }
}

public class CashierAuditDTO
{
    public string Cajero { get; set; } = "";
    public int Facturas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal Descuentos { get; set; }
    public decimal TicketPromedio => Facturas > 0 ? TotalVentas / Facturas : 0;
}

public class ArqueoInsightDTO
{
    public int IdTurno { get; set; }
    public string Usuario { get; set; } = "";
    public DateTime Apertura { get; set; }
    public DateTime? Cierre { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal VentasTransferencia { get; set; }
    public decimal VentasTarjeta { get; set; }
    public decimal SaldoTeorico { get; set; }
    public decimal SaldoReal { get; set; }
    public decimal Diferencia => SaldoReal - SaldoTeorico;
    public List<PaymentMethodStatDTO> DesglosePagos { get; set; } = new();
}

public class GarantiaInsightDTO
{
    public int Activas { get; set; }
    public int PorVencer { get; set; }
    public int Reclamadas { get; set; }
    public List<GarantiaDetalleDTO> Recientes { get; set; } = new();
}

public class GarantiaDetalleDTO
{
    public string Factura { get; set; } = "";
    public string Producto { get; set; } = "";
    public DateTime Vencimiento { get; set; }
    public string Estado { get; set; } = "";
}

public class SystemAlertDTO
{
    public string Titulo { get; set; } = "";
    public string Mensaje { get; set; } = "";
    public string Tipo { get; set; } = "info"; // info, warning, danger
    public DateTime Fecha { get; set; }
}

public class VentaTurnoDTO
{
    public int IdVenta { get; set; }
    public string NumeroFactura { get; set; } = "";
    public DateTime FechaVenta { get; set; }
    public string Cliente { get; set; } = "";
    public decimal TotalNio { get; set; }
    public string MetodoPago { get; set; } = "";
    public bool Anulada { get; set; }
}

public class CategoryStatDTO
{
    public string Categoria { get; set; } = "";
    public decimal Total { get; set; }
}

public class MovimientoTurnoDTO
{
    public string TipoMovimiento { get; set; } = ""; // "Venta", "Ingreso", "Egreso"
    public string Referencia { get; set; } = ""; // Factura # o Concepto
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = ""; 
    public decimal Monto { get; set; }
    public decimal Vuelto { get; set; } 
    public string MetodoPago { get; set; } = "";
    public string Estado { get; set; } = ""; // EFECTUADA, ANULADA, COMPLETADO
}



