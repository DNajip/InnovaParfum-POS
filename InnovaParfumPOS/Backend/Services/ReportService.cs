using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InnovaParfumPOS.Backend.Models;

namespace InnovaParfumPOS.Backend.Services;

public interface IReportService
{
    Task<DashboardStatsDTO> GetDashboardStatsAsync(DateTime start, DateTime end);
    Task<List<TrendPointDTO>> GetSalesTrendsAsync(DateTime start, DateTime end);
    Task<List<PaymentMethodStatDTO>> GetPaymentMethodStatsAsync(DateTime start, DateTime end);
    Task<List<TopProductoDTO>> GetTopProductosAsync(DateTime start, DateTime end, int count = 5);
    Task<List<ResumenDiarioDTO>> GetResumenDiarioAsync(DateTime start, DateTime end);
    Task<List<HourlySalesDTO>> GetHourlySalesAsync(DateTime start, DateTime end);
    Task<InventoryInsightDTO> GetInventoryInsightsAsync();
    Task<List<ClientInsightDTO>> GetClientInsightsAsync(DateTime start, DateTime end);
    Task<List<CashierAuditDTO>> GetCashierAuditAsync(DateTime start, DateTime end);
    Task<List<ArqueoInsightDTO>> GetArqueoInsightsAsync(DateTime start, DateTime end);
    Task<List<MovimientoTurnoDTO>> GetMovimientosPorTurnoAsync(int idTurno);
    Task<GarantiaInsightDTO> GetGarantiaStatsAsync();
    Task<List<CategoryStatDTO>> GetCategorySalesAsync(DateTime start, DateTime end);
    Task<List<SystemAlertDTO>> GetSystemAlertsAsync();
    Task<VClienteDashboardStat> GetClienteDashboardStatsAsync();
    Task<List<Movimiento>> GetKardexGlobalAsync(DateTime start, DateTime end);
}

public class ReportService : IReportService
{
    private readonly InnovaParfumDbContext _context;
    private readonly AppState _appState;

    public ReportService(InnovaParfumDbContext context, AppState appState)
    {
        _context = context;
        _appState = appState;
    }

    public async Task<DashboardStatsDTO> GetDashboardStatsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var currentVentas = await _context.Ventas
            .AsNoTracking()
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .Include(v => v.Pagos) // Para metricas de pago
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .ToListAsync();

        var todasLasVentasParaAnulaciones = await _context.Ventas
            .AsNoTracking()
            .Include(v => v.VentaDetalles)
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end)
            .ToListAsync();

        var reversosMovimientos = await _context.MovimientosVarios
            .AsNoTracking()
            .Where(m => m.Fecha >= start && m.Fecha <= end && m.Tipo == "EGRESO" && m.Concepto.StartsWith("Reverso"))
            .ToListAsync();

        var turnos = await _context.Turnos
            .AsNoTracking()
            .Where(t => t.FechaApertura >= start && t.FechaApertura <= end && t.IdEstado == 2)
            .ToListAsync();

        // Calcular periodo anterior equivalente
        var days = (end - start).TotalDays;
        if (days < 1) days = 1; 
        
        var prevStart = start.AddDays(-days);
        var prevEnd = start.AddTicks(-1);

        var prevVentas = await _context.Ventas
            .Where(v => v.FechaVenta >= prevStart && v.FechaVenta <= prevEnd && !v.Anulada)
            .ToListAsync();

        var regaliasDetalles = currentVentas.SelectMany(v => v.VentaDetalles
            .Where(d => d.PrecioUnitarioBase == 0 && d.IdProductoNavigation != null)
            .Select(d => new { Detalle = d, Venta = v })).ToList();

        var configMoneda = await _context.Configuracions.FirstOrDefaultAsync(c => c.Clave == "Moneda_Principal");
        bool isBaseUsd = configMoneda?.Valor == "USD";

        var stats = new DashboardStatsDTO
        {
            VentasBrutas = currentVentas.Sum(v => isBaseUsd ? v.TotalBase * v.TasaCambioUsd : v.TotalBase),
            VentasBrutasUsd = currentVentas.Sum(v => isBaseUsd ? v.TotalBase : (v.TasaCambioUsd > 0 ? v.TotalBase / v.TasaCambioUsd : 0)),
            TotalFacturas = currentVentas.Count,
            ProductosVendidos = currentVentas.SelectMany(v => v.VentaDetalles).Sum(d => d.Cantidad),
            TicketPromedio = currentVentas.Any() ? currentVentas.Average(v => isBaseUsd ? v.TotalBase * v.TasaCambioUsd : v.TotalBase) : 0,
            TicketPromedioUsd = currentVentas.Any() ? currentVentas.Average(v => isBaseUsd ? v.TotalBase : (v.TasaCambioUsd > 0 ? v.TotalBase / v.TasaCambioUsd : 0)) : 0,
            UtilidadNeta = currentVentas.Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? (d.SubtotalBase * v.TasaCambioUsd) : d.SubtotalBase) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? v.TasaCambioUsd : 1)))),
            UtilidadNetaUsd = currentVentas.Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? d.SubtotalBase : (v.TasaCambioUsd > 0 ? d.SubtotalBase / v.TasaCambioUsd : 0)) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? 1 : (v.TasaCambioUsd > 0 ? 1 / v.TasaCambioUsd : 0))))),
            GananciaMinorista = currentVentas.Where(v => v.IdTipoVenta == 1).Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? (d.SubtotalBase * v.TasaCambioUsd) : d.SubtotalBase) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? v.TasaCambioUsd : 1)))),
            GananciaMayorista = currentVentas.Where(v => v.IdTipoVenta == 2).Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? (d.SubtotalBase * v.TasaCambioUsd) : d.SubtotalBase) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? v.TasaCambioUsd : 1)))),
            GananciaRealizada = currentVentas.Where(v => v.IdCondicionPago == 1).Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? (d.SubtotalBase * v.TasaCambioUsd) : d.SubtotalBase) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? v.TasaCambioUsd : 1)))),
            GananciaEstancada = currentVentas.Where(v => v.IdCondicionPago == 2).Sum(v => v.VentaDetalles.Sum(d => 
                (isBaseUsd ? (d.SubtotalBase * v.TasaCambioUsd) : d.SubtotalBase) - 
                ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad * (isBaseUsd ? v.TasaCambioUsd : 1)))),
            ClientesNuevos = await _context.Personas.CountAsync(p => p.FechaCreacion >= start && p.FechaCreacion <= end && p.EsCliente),
            Anulaciones = todasLasVentasParaAnulaciones.Count(v => v.Anulada),

            DescuentosNio = currentVentas.Sum(v => isBaseUsd ? v.DescuentoBase * v.TasaCambioUsd : v.DescuentoBase),
            DescuentosUsd = currentVentas.Sum(v => isBaseUsd ? v.DescuentoBase : (v.TasaCambioUsd > 0 ? v.DescuentoBase / v.TasaCambioUsd : 0)),
            FacturasConDescuento = currentVentas.Count(v => v.DescuentoBase > 0),

            RegaliasMinoristaNio = regaliasDetalles.Sum(r => isBaseUsd ? ((r.Detalle.IdProductoNavigation.PrecioMinorista ?? 0) * r.Detalle.Cantidad * r.Venta.TasaCambioUsd) : ((r.Detalle.IdProductoNavigation.PrecioMinorista ?? 0) * r.Detalle.Cantidad)),
            RegaliasMinoristaUsd = regaliasDetalles.Sum(r => isBaseUsd ? ((r.Detalle.IdProductoNavigation.PrecioMinorista ?? 0) * r.Detalle.Cantidad) : (r.Venta.TasaCambioUsd > 0 ? ((r.Detalle.IdProductoNavigation.PrecioMinorista ?? 0) * r.Detalle.Cantidad) / r.Venta.TasaCambioUsd : 0)),
            RegaliasMayoristaNio = regaliasDetalles.Sum(r => isBaseUsd ? ((r.Detalle.IdProductoNavigation.PrecioMayorista ?? 0) * r.Detalle.Cantidad * r.Venta.TasaCambioUsd) : ((r.Detalle.IdProductoNavigation.PrecioMayorista ?? 0) * r.Detalle.Cantidad)),
            RegaliasMayoristaUsd = regaliasDetalles.Sum(r => isBaseUsd ? ((r.Detalle.IdProductoNavigation.PrecioMayorista ?? 0) * r.Detalle.Cantidad) : (r.Venta.TasaCambioUsd > 0 ? ((r.Detalle.IdProductoNavigation.PrecioMayorista ?? 0) * r.Detalle.Cantidad) / r.Venta.TasaCambioUsd : 0)),
            FacturasRegalia = currentVentas.Count(v => v.VentaDetalles.Any(d => d.PrecioUnitarioBase == 0)),

            EfectivoMinoristaNio = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 1).Sum(p => p.MontoPagado),
            EfectivoMinoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 2).Sum(p => p.MontoPagado),
            EfectivoMayoristaNio = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 1).Sum(p => p.MontoPagado),
            EfectivoMayoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 2).Sum(p => p.MontoPagado),
            
            TarjetaMinoristaNio = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 3).Sum(p => p.MontoPagado),
            TarjetaMinoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 1005).Sum(p => p.MontoPagado),
            TarjetaMayoristaNio = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 3).Sum(p => p.MontoPagado),
            TarjetaMayoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 1005).Sum(p => p.MontoPagado),
            
            TransferenciaMinoristaNio = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 4).Sum(p => p.MontoPagado),
            TransferenciaMinoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 1).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 5).Sum(p => p.MontoPagado),
            TransferenciaMayoristaNio = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 4).Sum(p => p.MontoPagado),
            TransferenciaMayoristaUsd = currentVentas.Where(v => v.IdTipoVenta == 2).SelectMany(v => v.Pagos).Where(p => p.IdMetodoPago == 5).Sum(p => p.MontoPagado),

            FacturasReversadas = todasLasVentasParaAnulaciones.Count(v => v.Anulada),
            MontoReversadoNio = reversosMovimientos.Where(m => m.IdMoneda == 1).Sum(m => m.Monto),
            MontoReversadoUsd = reversosMovimientos.Where(m => m.IdMoneda == 2).Sum(m => m.Monto),
            ArticulosReversados = todasLasVentasParaAnulaciones.SelectMany(v => v.VentaDetalles).Count(d => d.Devuelto),

            FaltantesNio = turnos.Where(t => t.EstadoCuadre == "Faltante").Sum(t => isBaseUsd ? Math.Abs(t.DiferenciaBase ?? 0) * (turnos.FirstOrDefault()?.Venta?.FirstOrDefault()?.TasaCambioUsd ?? _appState.ExchangeRateBuy) : Math.Abs(t.DiferenciaBase ?? 0)),
            FaltantesUsd = turnos.Where(t => t.EstadoCuadre == "Faltante").Sum(t => isBaseUsd ? Math.Abs(t.DiferenciaBase ?? 0) : Math.Abs(t.DiferenciaUsd ?? 0)),
            SobrantesNio = turnos.Where(t => t.EstadoCuadre == "Sobrante").Sum(t => isBaseUsd ? Math.Abs(t.DiferenciaBase ?? 0) * (turnos.FirstOrDefault()?.Venta?.FirstOrDefault()?.TasaCambioUsd ?? _appState.ExchangeRateBuy) : Math.Abs(t.DiferenciaBase ?? 0)),
            SobrantesUsd = turnos.Where(t => t.EstadoCuadre == "Sobrante").Sum(t => isBaseUsd ? Math.Abs(t.DiferenciaBase ?? 0) : Math.Abs(t.DiferenciaUsd ?? 0))
        };

        // Calcular porcentajes
        decimal prevVentasTotal = prevVentas.Sum(v => v.TotalBase);
        stats.PorcentajeVentas = CalcularVariacion(stats.VentasBrutas, prevVentasTotal);
        stats.PorcentajeFacturas = CalcularVariacion(stats.TotalFacturas, prevVentas.Count);
        
        decimal prevUtilidad = prevVentas.Sum(v => v.VentaDetalles.Sum(d => 
            d.SubtotalBase - ((d.CostoUnitarioNio ?? ((d.IdProductoNavigation?.CostoProducto ?? 0) + (d.IdProductoNavigation?.CostoEnvio ?? 0))) * d.Cantidad)));
        stats.PorcentajeUtilidad = CalcularVariacion(stats.UtilidadNeta, prevUtilidad);
        
        decimal prevTicket = prevVentas.Any() ? prevVentas.Average(v => v.TotalBase) : 0;
        stats.PorcentajeTicket = CalcularVariacion(stats.TicketPromedio, prevTicket);

        var prevClientes = await _context.Personas.CountAsync(p => p.FechaCreacion >= prevStart && p.FechaCreacion <= prevEnd && p.EsCliente);
        stats.PorcentajeClientes = CalcularVariacion(stats.ClientesNuevos, prevClientes);

        return stats;
    }

    public async Task<List<TrendPointDTO>> GetSalesTrendsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var ventas = await _context.Ventas
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .OrderBy(v => v.FechaVenta)
            .ToListAsync();

        return ventas.GroupBy(v => v.FechaVenta.Date)
            .Select(g => new TrendPointDTO
            {
                Label = g.Key.ToString("dd MMM"),
                ValorBase = g.Sum(v => v.TotalBase),
                ValorUsd = g.Sum(v => v.TasaCambioUsd > 0 ? v.TotalBase / v.TasaCambioUsd : v.TotalBase / _appState.ExchangeRateBuy)
            })
            .ToList();
    }

    public async Task<List<PaymentMethodStatDTO>> GetPaymentMethodStatsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var pagos = await _context.Pagos
            .Include(p => p.IdMetodoPagoNavigation)
            .Include(p => p.IdVentaNavigation)
            .Where(p => p.FechaPago >= start && p.FechaPago <= end && !p.IdVentaNavigation.Anulada)
            .ToListAsync();

        decimal total = pagos.Sum(p => p.MontoEnBase);

        return pagos.GroupBy(p => p.IdMetodoPagoNavigation.Nombre)
            .Select(g => new PaymentMethodStatDTO
            {
                Metodo = g.Key,
                Total = g.Sum(p => p.MontoEnBase - (p.VueltoBase ?? 0)),
                Porcentaje = (double)(g.Sum(p => p.MontoEnBase - (p.VueltoBase ?? 0)) / (total > 0 ? total : 1) * 100)
            })
            .ToList();
    }

    public async Task<List<TopProductoDTO>> GetTopProductosAsync(DateTime start, DateTime end, int count = 5)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        return await _context.VentaDetalles
            .Include(d => d.IdVentaNavigation)
            .Where(d => d.IdVentaNavigation.FechaVenta >= start && d.IdVentaNavigation.FechaVenta <= end && !d.IdVentaNavigation.Anulada)
            .GroupBy(d => d.DescripcionSnap)
            .Select(g => new TopProductoDTO
            {
                Nombre = g.Key,
                Unidades = g.Sum(d => d.Cantidad),
                TotalVentas = g.Sum(d => d.SubtotalBase)
            })
            .OrderByDescending(x => x.TotalVentas)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ResumenDiarioDTO>> GetResumenDiarioAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var ventas = await _context.Ventas
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .ToListAsync();

        return ventas.GroupBy(v => v.FechaVenta.Date)
            .Select(g => new ResumenDiarioDTO
            {
                Fecha = g.Key,
                VentasBrutas = g.Sum(v => v.TotalBase),
                Devoluciones = 0, // Por implementar lógica de devoluciones real si existe
                VentasNetas = g.Sum(v => v.TotalBase),
                Facturas = g.Count(),
                TicketPromedio = g.Average(v => v.TotalBase)
            })
            .OrderByDescending(x => x.Fecha)
            .ToList();
    }

    public async Task<List<HourlySalesDTO>> GetHourlySalesAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var ventas = await _context.Ventas
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .ToListAsync();

        var result = new List<HourlySalesDTO>();
        for (int d = 0; d < 7; d++)
        {
            for (int h = 7; h <= 21; h++)
            {
                var total = ventas.Where(v => (int)v.FechaVenta.DayOfWeek == d && v.FechaVenta.Hour == h).Sum(v => v.TotalBase);
                result.Add(new HourlySalesDTO
                {
                    DayOfWeek = d,
                    Hour = h,
                    Total = total,
                    Intensity = 0 // Se calcula abajo
                });
            }
        }

        // Calcular intensidad relativa al máximo del dataset
        var maxTotal = result.Any() ? result.Max(r => r.Total) : 0;
        if (maxTotal > 0)
        {
            foreach (var item in result)
            {
                if (item.Total > 0)
                {
                    // Escala de 1-10 proporcional al máximo
                    item.Intensity = Math.Max(1, (int)Math.Ceiling((double)(item.Total / maxTotal * 10)));
                }
            }
        }

        return result;
    }

    public async Task<InventoryInsightDTO> GetInventoryInsightsAsync()
    {
        var critico = await _context.Productos
            .Include(p => p.IdCategoriaNavigation)
            .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
            .Select(p => new VStockValorizadoDTO
            {
                IdProducto = p.IdProducto,
                Nombre = p.Nombre,
                Categoria = p.IdCategoriaNavigation != null ? p.IdCategoriaNavigation.Nombre : "Sin categoría",
                Marca = p.Marca ?? "",
                OrigenTipo = p.IdOrigenNavigation!.Nombre ?? "",
                Concentracion = p.IdConcentracionNavigation!.Nombre ?? "",
                StockActual = p.StockActual,
                StockMinimo = p.StockMinimo,
                EstadoStock = p.EstadoStock,
                CostoProducto = p.CostoProducto ?? 0m,
                PrecioMinorista = p.PrecioMinorista ?? 0
            })
            .ToListAsync();
        
        var allProducts = await _context.Productos.Where(p => p.Activo).ToListAsync();
        var valorCosto = allProducts.Sum(p => (p.CostoProducto ?? 0) * p.StockActual);
        var valorVenta = allProducts.Sum(p => (p.PrecioMinorista ?? 0) * p.StockActual);
        
        var fechaLimite = DateTime.Today.AddDays(-30);
        var productosSinVenta = await _context.Productos
            .Where(p => p.Activo && !_context.VentaDetalles.Any(d => d.IdProducto == p.IdProducto && d.IdVentaNavigation.FechaVenta >= fechaLimite))
            .Select(p => new
            {
                p.Nombre,
                p.FechaCreacion,
                UltimaVenta = _context.VentaDetalles
                    .Where(d => d.IdProducto == p.IdProducto)
                    .OrderByDescending(d => d.IdVentaNavigation.FechaVenta)
                    .Select(d => (DateTime?)d.IdVentaNavigation.FechaVenta)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var result = productosSinVenta.Select(p => new ProductNoMovementDTO
        {
            Nombre = p.Nombre,
            UltimaVenta = p.UltimaVenta,
            DiasSinVenta = (DateTime.Today - (p.UltimaVenta ?? p.FechaCreacion)).Days
        })
        .OrderByDescending(x => x.DiasSinVenta)
        .Take(10)
        .ToList();

        return new InventoryInsightDTO
        {
            StockCritico = critico,
            SinMovimiento = result,
            ValorTotalCosto = valorCosto,
            ValorTotalVenta = valorVenta
        };
    }

    public async Task<List<ClientInsightDTO>> GetClientInsightsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        
        var ventas = await _context.Ventas
            .Include(v => v.IdPersonaNavigation)
            .Include(v => v.IdCondicionPagoNavigation)
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada && v.IdPersona != null)
            .ToListAsync();

        var creditos = await _context.Creditos
            .Include(c => c.Venta)
            .Where(c => c.Venta.FechaVenta >= start && c.Venta.FechaVenta <= end && !c.Venta.Anulada && c.IdPersona != null)
            .ToListAsync();

        var groupedVentas = ventas.GroupBy(v => v.IdPersonaNavigation!.NombreCompleto);
        var result = new List<ClientInsightDTO>();

        foreach (var g in groupedVentas)
        {
            var nombre = g.Key ?? "Cliente General";
            var idPersona = g.First().IdPersona;
            
            var clienteCreditos = creditos.Where(c => c.IdPersona == idPersona).ToList();
            
            result.Add(new ClientInsightDTO
            {
                Nombre = nombre,
                TotalCompras = g.Count(),
                MontoTotal = g.Sum(v => v.TotalBase),
                ComprasContado = g.Count(v => v.IdCondicionPagoNavigation?.Descripcion?.ToUpper().Contains("CONTADO") == true || v.IdCondicionPago == 1),
                ComprasCredito = g.Count(v => v.IdCondicionPagoNavigation?.Descripcion?.ToUpper().Contains("CRÉDITO") == true || v.IdCondicionPagoNavigation?.Descripcion?.ToUpper().Contains("CREDITO") == true || v.IdCondicionPago == 2),
                SaldoPendiente = clienteCreditos.Sum(c => c.SaldoPendiente)
            });
        }

        return result.OrderByDescending(x => x.MontoTotal).Take(10).ToList();
    }

    public async Task<List<CashierAuditDTO>> GetCashierAuditAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        
        var ventas = await _context.Ventas
            .Include(v => v.IdUsuarioNavigation)
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end)
            .ToListAsync();
            
        var turnos = await _context.Turnos
            .Include(t => t.IdUsuarioNavigation)
            .Where(t => t.FechaApertura >= start && t.FechaApertura <= end)
            .ToListAsync();

        var users = ventas.Select(v => v.IdUsuarioNavigation?.Username)
            .Concat(turnos.Select(t => t.IdUsuarioNavigation?.Username))
            .Where(u => u != null)
            .Distinct();

        var auditList = new List<CashierAuditDTO>();

        foreach(var u in users)
        {
            var userVentas = ventas.Where(v => v.IdUsuarioNavigation?.Username == u).ToList();
            var userTurnos = turnos.Where(t => t.IdUsuarioNavigation?.Username == u).ToList();
            
            auditList.Add(new CashierAuditDTO
            {
                Cajero = u ?? "Sistema",
                Facturas = userVentas.Count(v => !v.Anulada),
                TotalVentas = userVentas.Where(v => !v.Anulada).Sum(v => v.TotalBase),
                Descuentos = userVentas.Where(v => !v.Anulada).Sum(v => v.DescuentoBase),
                Anulaciones = userVentas.Count(v => v.Anulada),
                DiferenciaArqueos = userTurnos.Sum(t => t.DiferenciaBase ?? 0)
            });
        }

        return auditList.OrderByDescending(x => x.TotalVentas).ToList();
    }

    public async Task<List<ArqueoInsightDTO>> GetArqueoInsightsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var turnos = await _context.Turnos
            .AsNoTracking()
            .Include(t => t.IdUsuarioNavigation)
                .ThenInclude(u => u.IdEmpleadoNavigation)
                    .ThenInclude(e => e.IdPersonaNavigation)
            .Include(t => t.MovimientosVarios)
            .Include(t => t.Venta)
                .ThenInclude(v => v.Pagos)
                    .ThenInclude(p => p.IdMetodoPagoNavigation)
            .Where(t => t.FechaApertura <= end && (t.FechaCierre == null || t.FechaCierre >= start))
            .OrderByDescending(t => t.FechaApertura)
            .ToListAsync();

          DateTime minDate = turnos.Any() ? turnos.Min(t => t.FechaApertura) : start;
          DateTime maxDate = turnos.Any(t => t.FechaCierre == null) ? DateTime.MaxValue : (turnos.Any() ? turnos.Max(t => t.FechaCierre) ?? end : end);

          var abonos = await _context.CreditoAbonos
              .AsNoTracking()
              .Include(a => a.MetodoPago)
              .Where(a => turnos.Any() && a.Fecha >= minDate && a.Fecha <= maxDate)
              .ToListAsync();

        return turnos.Select(t => {
            var ventasValidas = t.Venta.Where(v => !v.Anulada).ToList();
            var ventasAnuladas = t.Venta.Where(v => v.Anulada).ToList();
            
            // Ventas Netas (Base)
            decimal ventasEfectuadasBase = ventasValidas.Sum(v => v.TotalBase);
            decimal ventasAnuladasBase = ventasAnuladas.Sum(v => v.TotalBase);

            // Cobros y Vueltos físicos (De TODAS las ventas, incluyendo anuladas, porque el dinero entró y salió físicamente de gaveta)
            var todosPagos = t.Venta.SelectMany(v => v.Pagos).ToList();
            
            // Abonos del turno (Mismo usuario, dentro de la franja horaria)
            var abonosTurno = abonos.Where(a => a.IdUsuario == t.IdUsuario && a.Fecha >= t.FechaApertura && a.Fecha <= (t.FechaCierre ?? DateTime.MaxValue)).ToList();

            // Desglose de Cobros Físicos (Efectivo) + Electrónicos (SIN convertir a base, sumando MontoPagado que es la moneda física)
            decimal efectivoVentasNIO = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("EFECTIVO") && p.IdMetodoPagoNavigation.IdMoneda == 1).Sum(p => p.MontoPagado);
            decimal efectivoVentasUSD = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("EFECTIVO") && p.IdMetodoPagoNavigation.IdMoneda == 2).Sum(p => p.MontoPagado);
            
            decimal efectivoAbonosNIO = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("EFECTIVO") && a.MetodoPago.IdMoneda == 1).Sum(a => a.MontoRecibidoMoneda);
            decimal efectivoAbonosUSD = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("EFECTIVO") && a.MetodoPago.IdMoneda == 2).Sum(a => a.MontoRecibidoMoneda);

            decimal transfVentasNIO = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("TRANSFERENCIA") && p.IdMetodoPagoNavigation.IdMoneda == 1).Sum(p => p.MontoPagado);
            decimal transfAbonosNIO = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("TRANSFERENCIA") && a.MetodoPago.IdMoneda == 1).Sum(a => a.MontoRecibidoMoneda);

            decimal transfVentasUSD = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("TRANSFERENCIA") && p.IdMetodoPagoNavigation.IdMoneda == 2).Sum(p => p.MontoPagado);
            decimal transfAbonosUSD = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("TRANSFERENCIA") && a.MetodoPago.IdMoneda == 2).Sum(a => a.MontoRecibidoMoneda);

            decimal tarjetaVentasNIO = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("TARJETA") && p.IdMetodoPagoNavigation.IdMoneda == 1).Sum(p => p.MontoPagado);
            decimal tarjetaAbonosNIO = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("TARJETA") && a.MetodoPago.IdMoneda == 1).Sum(a => a.MontoRecibidoMoneda);

            decimal tarjetaVentasUSD = todosPagos.Where(p => p.IdMetodoPagoNavigation.Nombre.Contains("TARJETA") && p.IdMetodoPagoNavigation.IdMoneda == 2).Sum(p => p.MontoPagado);
            decimal tarjetaAbonosUSD = abonosTurno.Where(a => a.MetodoPago.Nombre.Contains("TARJETA") && a.MetodoPago.IdMoneda == 2).Sum(a => a.MontoRecibidoMoneda);

            // Abonos a Crédito (Lo que el cliente pagó de sus deudas) - Agrupado por moneda
            decimal abonosCreditoNIO = abonosTurno.Where(a => a.MetodoPago.IdMoneda == 1).Sum(a => a.MontoRecibidoMoneda);
            decimal abonosCreditoUSD = abonosTurno.Where(a => a.MetodoPago.IdMoneda == 2).Sum(a => a.MontoRecibidoMoneda);

            // Vuelto / Cambio (Extraemos de TODAS las ventas porque si fue anulada, el vuelto ya se había entregado físicamente)
            decimal vueltoVentasNIO = t.Venta.Where(v => v.MonedaVuelto == "NIO").Sum(v => v.Pagos.Sum(p => p.VueltoMostrado ?? 0));
            decimal vueltoVentasUSD = t.Venta.Where(v => v.MonedaVuelto == "USD").Sum(v => v.Pagos.Sum(p => p.VueltoMostrado ?? 0));
            
            decimal vueltoAbonosNIO = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 1 && (m.Concepto ?? "").Contains("Vuelto de Abono")).Sum(m => m.Monto);
            decimal vueltoAbonosUSD = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 2 && (m.Concepto ?? "").Contains("Vuelto de Abono")).Sum(m => m.Monto);

            // Otros Movimientos
            decimal ingresosManualesNIO = t.MovimientosVarios.Where(m => m.Tipo == "INGRESO" && m.IdMoneda == 1 && !(m.Concepto ?? "").Contains("Abono a Cr")).Sum(m => m.Monto);
            decimal ingresosManualesUSD = t.MovimientosVarios.Where(m => m.Tipo == "INGRESO" && m.IdMoneda == 2 && !(m.Concepto ?? "").Contains("Abono a Cr")).Sum(m => m.Monto);

            decimal reversosNIO = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 1 && (m.Concepto ?? "").StartsWith("Reverso")).Sum(m => m.Monto);
            decimal reversosUSD = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 2 && (m.Concepto ?? "").StartsWith("Reverso")).Sum(m => m.Monto);

            decimal retirosManualesNIO = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 1 && !(m.Concepto ?? "").Contains("Vuelto de Abono") && !(m.Concepto ?? "").StartsWith("Reverso")).Sum(m => m.Monto);
            decimal retirosManualesUSD = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 2 && !(m.Concepto ?? "").Contains("Vuelto de Abono") && !(m.Concepto ?? "").StartsWith("Reverso")).Sum(m => m.Monto);

            // Caja Teórica (puramente física)
            decimal teoricoNIO = t.MontoInicialBase + efectivoVentasNIO + efectivoAbonosNIO + ingresosManualesNIO - retirosManualesNIO - reversosNIO - vueltoVentasNIO - vueltoAbonosNIO;
            decimal teoricoUSD = t.MontoInicialUsd + efectivoVentasUSD + efectivoAbonosUSD + ingresosManualesUSD - retirosManualesUSD - reversosUSD - vueltoVentasUSD - vueltoAbonosUSD;
            
            decimal realNIO = t.FechaCierre != null ? (t.MontoContadoBase ?? 0) : 0;
            decimal realUSD = t.FechaCierre != null ? (t.MontoContadoUsd ?? 0) : 0;

            var nombreCompleto = t.IdUsuarioNavigation?.IdEmpleadoNavigation?.IdPersonaNavigation?.NombreCompleto;
            var usuarioFinal = !string.IsNullOrWhiteSpace(nombreCompleto) ? nombreCompleto : (t.IdUsuarioNavigation?.Username ?? "Sistema");

            // --- CÁLCULO DE VENTAS NETAS (CONTADO) ---
            var ventasContado = ventasValidas.Where(v => v.IdCondicionPago == 1).ToList();
            var pagosContado = ventasContado.SelectMany(v => v.Pagos).ToList();
            
            decimal cobrosContadoNIO = pagosContado.Where(p => p.IdMetodoPagoNavigation.IdMoneda == 1).Sum(p => p.MontoPagado);
            decimal cobrosContadoUSD = pagosContado.Where(p => p.IdMetodoPagoNavigation.IdMoneda == 2).Sum(p => p.MontoPagado);
            
            decimal vueltoContadoNIO = ventasContado.Where(v => v.MonedaVuelto == "NIO").Sum(v => v.Pagos.Sum(p => p.VueltoMostrado ?? 0));
            decimal vueltoContadoUSD = ventasContado.Where(v => v.MonedaVuelto == "USD").Sum(v => v.Pagos.Sum(p => p.VueltoMostrado ?? 0));
            
            decimal reversosContadoNIO = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 1 && (m.Concepto ?? "").StartsWith("Reverso") && ventasContado.Any(v => (m.Concepto ?? "").Contains($"Fac {v.IdVenta} -"))).Sum(m => m.Monto);
            decimal reversosContadoUSD = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO" && m.IdMoneda == 2 && (m.Concepto ?? "").StartsWith("Reverso") && ventasContado.Any(v => (m.Concepto ?? "").Contains($"Fac {v.IdVenta} -"))).Sum(m => m.Monto);
            
            // Ventas Netas: El cliente solicitó que sea la suma del ingreso total recibido (sin restar el vuelto)
            decimal ventasNetasNIO = Math.Max(0, cobrosContadoNIO - reversosContadoNIO);
            decimal ventasNetasUSD = Math.Max(0, cobrosContadoUSD - reversosContadoUSD);

            return new ArqueoInsightDTO
            {
                IdTurno = t.IdTurno,
                Usuario = usuarioFinal,
                Apertura = t.FechaApertura,
                Cierre = t.FechaCierre,
                
                MontoInicialNIO = t.MontoInicialBase,
                MontoInicialUSD = t.MontoInicialUsd,

                VentasEfectuadasBase = ventasEfectuadasBase,
                CantVentasEfectuadas = ventasValidas.Count,

                VentasAnuladasBase = ventasAnuladasBase,
                CantVentasAnuladas = ventasAnuladas.Count,
                
                VentasNetasNIO = ventasNetasNIO,
                VentasNetasUSD = ventasNetasUSD,

                CobrosEfectivoNIO = efectivoVentasNIO,
                CobrosEfectivoUSD = efectivoVentasUSD,

                CobrosTransferenciaNIO = transfVentasNIO,
                CobrosTransferenciaUSD = transfVentasUSD,

                CobrosTarjetaNIO = tarjetaVentasNIO,
                CobrosTarjetaUSD = tarjetaVentasUSD,

                AbonosEfectivoNIO = efectivoAbonosNIO,
                AbonosEfectivoUSD = efectivoAbonosUSD,
                AbonosTransferenciaNIO = transfAbonosNIO,
                AbonosTransferenciaUSD = transfAbonosUSD,
                AbonosTarjetaNIO = tarjetaAbonosNIO,
                AbonosTarjetaUSD = tarjetaAbonosUSD,

                IngresosManualesNIO = ingresosManualesNIO,
                IngresosManualesUSD = ingresosManualesUSD,

                RetirosManualesNIO = retirosManualesNIO,
                RetirosManualesUSD = retirosManualesUSD,
                
                ReversosNIO = reversosNIO,
                ReversosUSD = reversosUSD,

                VueltoNIO = vueltoVentasNIO + vueltoAbonosNIO,
                VueltoUSD = vueltoVentasUSD + vueltoAbonosUSD,

                SaldoTeoricoNIO = teoricoNIO,
                SaldoTeoricoUSD = teoricoUSD,

                SaldoRealNIO = t.FechaCierre != null ? realNIO : null,
                SaldoRealUSD = t.FechaCierre != null ? realUSD : null,



                EstadoCuadreNIO = t.FechaCierre == null ? "ABIERTO" : ((realNIO - teoricoNIO) < 0 ? "FALTANTE" : ((realNIO - teoricoNIO) > 0 ? "SOBRANTE" : "CUADRADO")),
                EstadoCuadreUSD = t.FechaCierre == null ? "ABIERTO" : ((realUSD - teoricoUSD) < 0 ? "FALTANTE" : ((realUSD - teoricoUSD) > 0 ? "SOBRANTE" : "CUADRADO")),

                DesglosePagos = new()
            };
        }).ToList();
    }

    public async Task<List<MovimientoTurnoDTO>> GetMovimientosPorTurnoAsync(int idTurno)
    {
        var ventas = await _context.Ventas
            .Include(v => v.IdPersonaNavigation)
            .Include(v => v.Pagos)
                .ThenInclude(p => p.IdMetodoPagoNavigation)
            .Where(v => v.IdTurno == idTurno)
            .ToListAsync();

        var movimientos = await _context.MovimientosVarios
            .Where(m => m.IdTurno == idTurno)
            .ToListAsync();

        var result = new List<MovimientoTurnoDTO>();

        foreach (var v in ventas)
        {
            var pagoPrincipal = v.Pagos.OrderByDescending(p => p.MontoEnBase).FirstOrDefault();
            var metodoPago = pagoPrincipal?.IdMetodoPagoNavigation?.Nombre ?? "N/A";
            if (v.Pagos.Count > 1)
            {
                var metodos = v.Pagos.Select(p => p.IdMetodoPagoNavigation.Nombre).Distinct();
                metodoPago = string.Join(", ", metodos);
            }

            var montoPagadoFisico = v.Pagos.Sum(p => p.MontoPagado);
            var vueltoMostradoFisico = v.Pagos.Sum(p => p.VueltoMostrado ?? 0);
            var simboloPago = pagoPrincipal?.IdMetodoPagoNavigation?.IdMoneda == 1 ? "C$" : "$";
            var simboloVuelto = v.MonedaVuelto == "NIO" ? "C$" : "$";

            var reversoMovimientos = movimientos.Where(m => m.Tipo == "EGRESO" && (m.Concepto ?? "").StartsWith($"Reverso/Devolución de Fac {v.IdVenta} -")).ToList();
            var reversoFisico = reversoMovimientos.Sum(m => m.Monto);
            var motivoReverso = string.Join(" | ", reversoMovimientos.Select(m => {
                var parts = (m.Concepto ?? "").Split(" - ", 2);
                return parts.Length > 1 ? parts[1] : m.Concepto;
            }));

            result.Add(new MovimientoTurnoDTO
            {
                TipoMovimiento = v.TotalBase == 0 ? "Regalía" : "Venta",
                Referencia = v.NumeroFactura ?? $"FAC-{v.IdVenta}",
                Fecha = v.FechaVenta,
                Cliente = v.IdPersonaNavigation?.NombreCompleto ?? "Cliente de Contado",
                Descuento = v.DescuentoBase,
                Monto = v.TotalBase,
                MontoPagado = montoPagadoFisico,
                Vuelto = vueltoMostradoFisico,
                MontoReverso = reversoFisico,
                MotivoReverso = motivoReverso,
                MontoTotal = montoPagadoFisico - vueltoMostradoFisico - reversoFisico,
                SimboloMonedaPago = simboloPago,
                SimboloMonedaVuelto = simboloVuelto,
                SimboloMonedaMonto = "$",
                MetodoPago = metodoPago,
                Estado = v.Anulada ? "ANULADA" : "EFECTUADA"
            });
        }

        var turno = await _context.Turnos.FindAsync(idTurno);
        var abonos = new List<CreditoAbono>();
        if (turno != null)
        {
            var nextTurno = await _context.Turnos
                .Where(t => t.IdTurno > turno.IdTurno)
                .OrderBy(t => t.IdTurno)
                .FirstOrDefaultAsync();

            var minDate = turno.FechaApertura;
            var maxDate = nextTurno?.FechaApertura ?? DateTime.MaxValue;
            if (turno.FechaCierre != null && turno.FechaCierre < maxDate) maxDate = turno.FechaCierre.Value;

            abonos = await _context.CreditoAbonos
                .Include(a => a.Credito)
                    .ThenInclude(c => c.Persona)
                .Include(a => a.Credito)
                    .ThenInclude(c => c.Abonos)
                .Include(a => a.MetodoPago)
                .Where(a => a.Fecha >= minDate && a.Fecha <= maxDate)
                .ToListAsync();
        }

        foreach (var a in abonos)
        {
            var simboloPago = a.MetodoPago.IdMoneda == 1 ? "C$" : "$";
            result.Add(new MovimientoTurnoDTO
            {
                TipoMovimiento = "Ingreso",
                Referencia = $"Abono a Crédito #{a.IdCredito}",
                Fecha = a.Fecha,
                Cliente = a.Credito?.Persona?.NombreCompleto ?? "N/A",
                Descuento = 0,
                Monto = a.Monto,
                MontoPagado = a.MontoRecibidoMoneda,
                Vuelto = (a.MonedaVuelto == "USD") ? a.VueltoBase : (a.VueltoBase * a.TasaCambio),
                MontoReverso = 0,
                MotivoReverso = "",
                MontoTotal = a.Monto,
                SimboloMonedaPago = simboloPago,
                SimboloMonedaVuelto = (a.MonedaVuelto == "USD") ? "$" : "C$",
                SimboloMonedaMonto = "$",
                MetodoPago = a.MetodoPago.Nombre,
                Estado = "COMPLETADO",
                SaldoPendiente = a.Credito != null 
                    ? a.Credito.MontoOriginal - a.Credito.Abonos.Where(prev => prev.IdAbono < a.IdAbono).Sum(prev => prev.Monto) 
                    : 0
            });
        }

        var otrosMovimientos = movimientos.Where(m => 
            !(m.Tipo == "EGRESO" && (m.Concepto ?? "").StartsWith("Reverso/Devolución de Fac ")) &&
            !(m.Concepto ?? "").StartsWith("Abono a Crédito #") &&
            !(m.Concepto ?? "").StartsWith("Vuelto de Abono a Crédito #")
        );

        foreach (var m in otrosMovimientos)
        {
            var simboloMov = m.IdMoneda == 1 ? "C$" : "$";
            result.Add(new MovimientoTurnoDTO
            {
                TipoMovimiento = m.Tipo == "INGRESO" ? "Ingreso" : "Egreso",
                Referencia = m.Concepto,
                Fecha = m.Fecha,
                Cliente = "N/A",
                Descuento = 0,
                Monto = 0,
                MontoPagado = 0,
                Vuelto = 0,
                MontoReverso = 0,
                MontoTotal = m.Tipo == "INGRESO" ? m.Monto : -m.Monto,
                SimboloMonedaPago = simboloMov,
                SimboloMonedaVuelto = simboloMov,
                SimboloMonedaMonto = simboloMov,
                MetodoPago = "EFECTIVO",
                Estado = "COMPLETADO"
            });
        }

        return result.OrderByDescending(x => x.Fecha).ToList();
    }

    public async Task<GarantiaInsightDTO> GetGarantiaStatsAsync()
    {
        var now = DateTime.Now;
        var nowOnly = DateOnly.FromDateTime(now);
        var detalles = await _context.VentaDetalles
            .Where(d => d.FechaVenceGarantia != null)
            .ToListAsync();

        var activas = detalles.Count(d => d.FechaVenceGarantia > nowOnly);
        var porVencer = detalles.Count(d => d.FechaVenceGarantia > nowOnly && d.FechaVenceGarantia < nowOnly.AddDays(7));

        return new GarantiaInsightDTO
        {
            Activas = activas,
            PorVencer = porVencer,
            Reclamadas = 0,
            Recientes = detalles.OrderByDescending(d => d.FechaVenceGarantia)
                .Take(5)
                .Select(d => new GarantiaDetalleDTO
                {
                    Factura = "FAC-" + d.IdVenta,
                    Producto = d.DescripcionSnap,
                    Vencimiento = d.FechaVenceGarantia.HasValue ? new DateTime(d.FechaVenceGarantia.Value.Year, d.FechaVenceGarantia.Value.Month, d.FechaVenceGarantia.Value.Day) : now,
                    Estado = d.FechaVenceGarantia > nowOnly ? "Activa" : "Vencida"
                }).ToList()
        };
    }

    public async Task<List<CategoryStatDTO>> GetCategorySalesAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        return await _context.VentaDetalles
            .Include(d => d.IdVentaNavigation)
            .Include(d => d.IdProductoNavigation)
                .ThenInclude(p => p.IdCategoriaNavigation)
            .Where(d => d.IdVentaNavigation.FechaVenta >= start && d.IdVentaNavigation.FechaVenta <= end && !d.IdVentaNavigation.Anulada)
            .GroupBy(d => d.IdProductoNavigation.IdCategoriaNavigation != null ? d.IdProductoNavigation.IdCategoriaNavigation.Nombre : "Sin categoría")
            .Select(g => new CategoryStatDTO
            {
                Categoria = g.Key ?? "Otros",
                Total = g.Sum(d => d.SubtotalBase)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();
    }

    public async Task<List<SystemAlertDTO>> GetSystemAlertsAsync()
    {
        var alerts = new List<SystemAlertDTO>();
        
        var stockCritico = await _context.VStockCriticos.CountAsync();
        if (stockCritico > 0)
        {
            alerts.Add(new SystemAlertDTO {
                Titulo = "Stock Crítico Detectado",
                Mensaje = $"Hay {stockCritico} productos por debajo del mínimo.",
                Tipo = "danger",
                Fecha = DateTime.Now
            });
        }

        return alerts;
    }

    public async Task<VClienteDashboardStat> GetClienteDashboardStatsAsync()
    {
        return await _context.VClienteDashboardStats.FirstOrDefaultAsync() 
            ?? new VClienteDashboardStat { TotalClientes = 0, TotalGarantiasActivas = 0, ClientesConComprasRecientes = 0 };
    }

    private decimal CalcularVariacion(decimal actual, decimal anterior)
    {
        if (anterior == 0) return actual > 0 ? 100 : 0;
        return ((actual - anterior) / anterior) * 100;
    }

    public async Task<List<Movimiento>> GetKardexGlobalAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        return await _context.Movimientos
            .Include(m => m.IdProductoNavigation)
            .Include(m => m.IdTipoMovNavigation)
            .Where(m => m.FechaMov >= start && m.FechaMov <= end)
            .OrderByDescending(m => m.FechaMov)
            .ToListAsync();
    }
}











