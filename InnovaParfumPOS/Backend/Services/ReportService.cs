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

    public ReportService(InnovaParfumDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDTO> GetDashboardStatsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var currentVentas = await _context.Ventas
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .ToListAsync();

        // Calcular periodo anterior equivalente
        var days = (end - start).TotalDays;
        if (days < 1) days = 1; 
        
        var prevStart = start.AddDays(-days);
        var prevEnd = start.AddTicks(-1);

        var prevVentas = await _context.Ventas
            .Where(v => v.FechaVenta >= prevStart && v.FechaVenta <= prevEnd && !v.Anulada)
            .ToListAsync();

        var stats = new DashboardStatsDTO
        {
            VentasBrutas = currentVentas.Sum(v => v.TotalNio),
            TotalFacturas = currentVentas.Count,
            ProductosVendidos = currentVentas.SelectMany(v => v.VentaDetalles).Sum(d => d.Cantidad),
            TicketPromedio = currentVentas.Any() ? currentVentas.Average(v => v.TotalNio) : 0,
            UtilidadNeta = currentVentas.SelectMany(v => v.VentaDetalles).Sum(d => 
                d.SubtotalNio - ((d.IdProductoNavigation?.PrecioCompra ?? 0) * d.Cantidad)),
            ClientesNuevos = await _context.Personas.CountAsync(p => p.FechaCreacion >= start && p.FechaCreacion <= end && p.EsCliente),
            Anulaciones = await _context.Ventas.CountAsync(v => v.FechaVenta >= start && v.FechaVenta <= end && v.Anulada)
        };

        // Calcular porcentajes
        decimal prevVentasTotal = prevVentas.Sum(v => v.TotalNio);
        stats.PorcentajeVentas = CalcularVariacion(stats.VentasBrutas, prevVentasTotal);
        stats.PorcentajeFacturas = CalcularVariacion(stats.TotalFacturas, prevVentas.Count);
        
        decimal prevUtilidad = prevVentas.SelectMany(v => v.VentaDetalles).Sum(d => 
            d.SubtotalNio - ((d.IdProductoNavigation?.PrecioCompra ?? 0) * d.Cantidad));
        stats.PorcentajeUtilidad = CalcularVariacion(stats.UtilidadNeta, prevUtilidad);
        
        decimal prevTicket = prevVentas.Any() ? prevVentas.Average(v => v.TotalNio) : 0;
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
                ValorNio = g.Sum(v => v.TotalNio),
                ValorUsd = g.Sum(v => v.TotalNio / 36.5m) // Asumiendo tasa fija por ahora para el gráfico
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

        decimal total = pagos.Sum(p => p.MontoEnNio);

        return pagos.GroupBy(p => p.IdMetodoPagoNavigation.Nombre)
            .Select(g => new PaymentMethodStatDTO
            {
                Metodo = g.Key,
                Total = g.Sum(p => p.MontoEnNio - (p.VueltoNio ?? 0)),
                Porcentaje = (double)(g.Sum(p => p.MontoEnNio - (p.VueltoNio ?? 0)) / (total > 0 ? total : 1) * 100)
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
                TotalVentas = g.Sum(d => d.SubtotalNio)
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
                VentasBrutas = g.Sum(v => v.TotalNio),
                Devoluciones = 0, // Por implementar lógica de devoluciones real si existe
                VentasNetas = g.Sum(v => v.TotalNio),
                Facturas = g.Count(),
                TicketPromedio = g.Average(v => v.TotalNio)
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
                var total = ventas.Where(v => (int)v.FechaVenta.DayOfWeek == d && v.FechaVenta.Hour == h).Sum(v => v.TotalNio);
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
                OrigenTipo = p.IdOrigenNavigation.Nombre ?? "",
                Concentracion = p.IdConcentracionNavigation.Nombre ?? "",
                StockActual = p.StockActual,
                StockMinimo = p.StockMinimo,
                EstadoStock = p.EstadoStock,
                PrecioCompra = p.PrecioCompra ?? 0m,
                PrecioVenta = p.PrecioVenta
            })
            .ToListAsync();
        
        var valorCosto = critico.Sum(p => p.ValorCostoTotal);
        var valorVenta = critico.Sum(p => p.ValorVentaTotal);
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
        return await _context.Ventas
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada && v.IdPersona != null)
            .GroupBy(v => v.IdPersonaNavigation!.NombreCompleto)
            .Select(g => new ClientInsightDTO
            {
                Nombre = g.Key ?? "Cliente General",
                TotalCompras = g.Count(),
                MontoTotal = g.Sum(v => v.TotalNio)
            })
            .OrderByDescending(x => x.MontoTotal)
            .Take(10)
            .ToListAsync();
    }

    public async Task<List<CashierAuditDTO>> GetCashierAuditAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var data = await _context.Ventas
            .Include(v => v.IdUsuarioNavigation)
            .Where(v => v.FechaVenta >= start && v.FechaVenta <= end && !v.Anulada)
            .GroupBy(v => v.IdUsuarioNavigation.Username)
            .Select(g => new
            {
                Username = g.Key ?? "N/A",
                FacturasGeneradas = g.Count(),
                TotalVentas = g.Sum(v => v.TotalNio),
                DescuentosAplicados = g.Sum(v => v.DescuentoNio)
            })
            .OrderByDescending(x => x.TotalVentas)
            .ToListAsync();

        return data.Select(g => new CashierAuditDTO
        {
            Cajero = g.Username,
            Facturas = g.FacturasGeneradas,
            TotalVentas = g.TotalVentas,
            Descuentos = g.DescuentosAplicados
        }).ToList();
    }

    public async Task<List<ArqueoInsightDTO>> GetArqueoInsightsAsync(DateTime start, DateTime end)
    {
        end = end.Date.AddDays(1).AddTicks(-1);
        var turnos = await _context.Turnos
            .Include(t => t.IdUsuarioNavigation)
            .Include(t => t.MovimientosVarios)
            .Include(t => t.Venta)
                .ThenInclude(v => v.Pagos)
                    .ThenInclude(p => p.IdMetodoPagoNavigation)
            .Where(t => t.FechaApertura <= end && (t.FechaCierre == null || t.FechaCierre >= start))
            .OrderByDescending(t => t.FechaApertura)
            .ToListAsync();

        return turnos.Select(t => {
            decimal ingresosVarios = t.MovimientosVarios.Where(m => m.Tipo == "INGRESO").Sum(m => m.Monto);
            decimal salidasVarias = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO").Sum(m => m.Monto);
            
            var desglose = t.Venta.Where(v => !v.Anulada)
                .SelectMany(v => v.Pagos)
                .GroupBy(p => p.IdMetodoPagoNavigation.Nombre)
                .Select(g => new PaymentMethodStatDTO
                {
                    Metodo = g.Key,
                    Total = g.Sum(p => p.MontoEnNio - (p.VueltoNio ?? 0))
                }).ToList();

            return new ArqueoInsightDTO
            {
                IdTurno = t.IdTurno,
                Usuario = t.IdUsuarioNavigation?.Username ?? "Sistema",
                Apertura = t.FechaApertura,
                Cierre = t.FechaCierre,
                MontoInicial = t.MontoInicialNio,
                VentasEfectivo = t.TotalEfectivoNio,
                VentasTransferencia = t.TotalTransferencia,
                VentasTarjeta = t.TotalTarjeta,
                SaldoTeorico = t.MontoInicialNio + t.TotalEfectivoNio + ingresosVarios - salidasVarias,
                SaldoReal = t.MontoContadoNio ?? 0,
                DesglosePagos = desglose
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
            var pagoPrincipal = v.Pagos.OrderByDescending(p => p.MontoEnNio).FirstOrDefault();
            var metodoPago = pagoPrincipal?.IdMetodoPagoNavigation?.Nombre ?? "N/A";
            if (v.Pagos.Count > 1)
            {
                var metodos = v.Pagos.Select(p => p.IdMetodoPagoNavigation.Nombre).Distinct();
                metodoPago = string.Join(", ", metodos);
            }

            var totalVuelto = v.Pagos.Sum(p => p.VueltoNio ?? 0);

            result.Add(new MovimientoTurnoDTO
            {
                TipoMovimiento = "Venta",
                Referencia = v.NumeroFactura ?? $"FAC-{v.IdVenta}",
                Fecha = v.FechaVenta,
                Cliente = v.IdPersonaNavigation?.NombreCompleto ?? "Cliente de Contado",
                Monto = v.TotalNio,
                Vuelto = totalVuelto,
                MetodoPago = metodoPago,
                Estado = v.Anulada ? "ANULADA" : "EFECTUADA"
            });
        }

        foreach (var m in movimientos)
        {
            result.Add(new MovimientoTurnoDTO
            {
                TipoMovimiento = m.Tipo == "INGRESO" ? "Ingreso" : "Egreso",
                Referencia = m.Concepto,
                Fecha = m.Fecha,
                Cliente = "N/A",
                Monto = m.Monto,
                Vuelto = 0,
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
                Total = g.Sum(d => d.SubtotalNio)
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





