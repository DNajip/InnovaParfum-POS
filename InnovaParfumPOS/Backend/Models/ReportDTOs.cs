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
    
    // Nuevas metricas de auditoria
    public decimal GananciaRealizada { get; set; }
    public decimal GananciaEstancada { get; set; }
    public decimal GananciaMayorista { get; set; }
    public decimal GananciaMinorista { get; set; }
}

public class TrendPointDTO
{
    public string Label { get; set; } = string.Empty;
    public decimal ValorBase { get; set; }
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
    public int ComprasContado { get; set; }
    public int ComprasCredito { get; set; }
    public decimal SaldoPendiente { get; set; }
}

public class CashierAuditDTO
{
    public string Cajero { get; set; } = "";
    public int Facturas { get; set; }
    public decimal TotalVentas { get; set; }
    public decimal Descuentos { get; set; }
    public decimal TicketPromedio => Facturas > 0 ? TotalVentas / Facturas : 0;
    
    // Metricas de Riesgo
    public int Anulaciones { get; set; }
    public decimal DiferenciaArqueos { get; set; }
    
    public string NivelRiesgo 
    {
        get
        {
            if (Anulaciones > 5 || DiferenciaArqueos < -500) return "ROJO";
            if (Anulaciones > 2 || DiferenciaArqueos < -100) return "AMARILLO";
            return "VERDE";
        }
    }
}

public class ArqueoInsightDTO
{
    public int IdTurno { get; set; }
    public string Usuario { get; set; } = "";
    public DateTime Apertura { get; set; }
    public DateTime? Cierre { get; set; }

    // IDENTIFICACIÃ“N
    public decimal MontoInicialNIO { get; set; }
    public decimal MontoInicialUSD { get; set; }

    // VENTAS (SÃ³lo base)
    public decimal VentasEfectuadasBase { get; set; }
    public int CantVentasEfectuadas { get; set; }

    public decimal VentasAnuladasBase { get; set; }
    public int CantVentasAnuladas { get; set; }

    public decimal VentasNetasBase => VentasEfectuadasBase - VentasAnuladasBase;

    // COBROS
    public decimal CobrosEfectivoNIO { get; set; }
    public decimal CobrosEfectivoUSD { get; set; }

    public decimal CobrosTransferenciaNIO { get; set; }
    public decimal CobrosTransferenciaUSD { get; set; }

    public decimal CobrosTarjetaNIO { get; set; }
    public decimal CobrosTarjetaUSD { get; set; }

    // ABONOS
    public decimal AbonosEfectivoNIO { get; set; }
    public decimal AbonosEfectivoUSD { get; set; }
    public decimal AbonosTransferenciaNIO { get; set; }
    public decimal AbonosTransferenciaUSD { get; set; }
    public decimal AbonosTarjetaNIO { get; set; }
    public decimal AbonosTarjetaUSD { get; set; }

    // OTROS MOVIMIENTOS
    public decimal IngresosManualesNIO { get; set; }
    public decimal IngresosManualesUSD { get; set; }

    public decimal RetirosManualesNIO { get; set; }
    public decimal RetirosManualesUSD { get; set; }
    public decimal ReversosNIO { get; set; }
    public decimal ReversosUSD { get; set; }

    public decimal VueltoNIO { get; set; }
    public decimal VueltoUSD { get; set; }

    // CAJA
    public decimal SaldoTeoricoNIO { get; set; }
    public decimal SaldoTeoricoUSD { get; set; }

    public decimal? SaldoRealNIO { get; set; }
    public decimal? SaldoRealUSD { get; set; }

    public decimal? DiferenciaNIO => SaldoRealNIO.HasValue ? SaldoRealNIO - SaldoTeoricoNIO : null;
    public decimal? DiferenciaUSD => SaldoRealUSD.HasValue ? SaldoRealUSD - SaldoTeoricoUSD : null;

    public string? EstadoCuadreNIO { get; set; }
    public string? EstadoCuadreUSD { get; set; }

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
    public decimal TotalBase { get; set; }
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
    public string TipoMovimiento { get; set; } = "";
    public string Referencia { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Cliente { get; set; } = ""; 
    public decimal Monto { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal Vuelto { get; set; } 
    public decimal MontoReverso { get; set; }
    public string MotivoReverso { get; set; } = "";
    public decimal MontoTotal { get; set; }
    public string SimboloMonedaPago { get; set; } = "$";
    public string SimboloMonedaVuelto { get; set; } = "$";
    public string SimboloMonedaMonto { get; set; } = "$";
    public string MetodoPago { get; set; } = "";
    public string Estado { get; set; } = "";
}

