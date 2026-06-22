using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovaParfumPOS.Backend.Services;

public interface ICheckoutService
{
    Task<Venta> ProcessCheckoutAsync(int userId, int? idPersona, int idTipoVenta, int idCondicionPago, decimal discount, List<CartItem> items, List<PaymentInput> payments, string monedaVuelto = "NIO");
    Task<List<PeriodosGarantium>> GetPeriodosGarantiaAsync();
    Task<List<MetodosPago>> GetMetodosPagoAsync();
    Task ReversarTransaccionAsync(int idVenta, int idUsuario, string motivo, string? detalleJson);
}

public class CheckoutService : ICheckoutService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;
    private readonly IShiftService _shiftService;

    public CheckoutService(IDbContextFactory<InnovaParfumDbContext> factory, IShiftService shiftService)
    {
        _factory = factory;
        _shiftService = shiftService;
    }

    public async Task<List<PeriodosGarantium>> GetPeriodosGarantiaAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.PeriodosGarantia
            .Where(p => p.IdEstado == 1)
            .OrderBy(p => p.Meses)
            .ToListAsync();
    }

    public async Task<List<MetodosPago>> GetMetodosPagoAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.MetodosPagos
            .Include(m => m.IdMonedaNavigation)
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }

    public async Task<Venta> ProcessCheckoutAsync(int userId, int? idPersona, int idTipoVenta, int idCondicionPago, decimal discount, List<CartItem> items, List<PaymentInput> payments, string monedaVuelto = "NIO")
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // 1. Map items to include dynamic UnitPrice based on TipoVenta
        var itemsMapped = items.Select(i => new {
            i.IdProducto,
            i.Description,
            UnitPrice = i.IsRegalia ? 0 : (idTipoVenta == 2 ? i.PrecioMayorista : i.PrecioMinorista),
            SubTotal = i.IsRegalia ? 0 : ((idTipoVenta == 2 ? i.PrecioMayorista : i.PrecioMinorista) * i.Quantity),
            IsRegalia = i.IsRegalia,
            i.Details
        });
        var itemsJson = System.Text.Json.JsonSerializer.Serialize(itemsMapped);
        
        // Mapeamos pagos para asegurar que las propiedades coincidan con el SP
        var paymentsMapped = payments.Select(p => new {
            p.IdMetodoPago,
            p.Monto,
            p.TasaCambio,
            MontoEnBase = p.MontoEnBase, // El monto bruto recibido en moneda nacional, calculado por el cliente
            MontoEnMonedaBase = p.MontoEnMonedaBase,
            VueltoMostrado = p.VueltoMostrado,
            p.Referencia
        });
        var paymentsJson = System.Text.Json.JsonSerializer.Serialize(paymentsMapped);

        try 
        {
            // Validacion de Credito
            if (idCondicionPago == 2) // CREDITO
            {
                if (!idPersona.HasValue) throw new Exception("Una venta a crédito requiere seleccionar un cliente.");
                
                var perfilCredito = await context.ClientesCredito.FirstOrDefaultAsync(c => c.IdPersona == idPersona.Value);
                if (perfilCredito == null || !perfilCredito.Activo)
                    throw new Exception("El cliente no tiene un perfil de crédito activo.");

                decimal totalVenta = itemsMapped.Sum(i => i.SubTotal) - discount;
                decimal limitDisponible = perfilCredito.LimiteCredito - perfilCredito.SaldoActual;

                if (totalVenta > limitDisponible)
                    throw new Exception($"Límite de crédito excedido. Disponible: {limitDisponible:C}, Total Venta: {totalVenta:C}");
            }

            // 2. Ejecutar el SP Maestro de Venta (ahora requiere idTipoVenta e idCondicionPago)
            var result = await context.Ventas
                .FromSqlRaw("SET QUOTED_IDENTIFIER ON; EXEC VEN.sp_ProcesarVenta @IdUsuario={0}, @IdPersona={1}, @IdTipoVenta={2}, @IdCondicionPago={3}, @DescuentoBase={4}, @TasaCambioUsd={5}, @ItemsJson={6}, @PaymentsJson={7}, @MonedaVuelto={8}",
                    userId,
                    idPersona ?? (object)DBNull.Value,
                    idTipoVenta,
                    idCondicionPago,
                    discount,
                    36.60m, // Podría venir de configuración
                    itemsJson,
                    paymentsJson,
                    monedaVuelto)
                .AsNoTracking()
                .ToListAsync();

            if (!result.Any())
                throw new Exception("La base de datos no devolvió el registro de la venta.");

            return result.First();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error en Checkout (DB): {ex.Message}");
        }
    }

    public async Task ReversarTransaccionAsync(int idVenta, int idUsuario, string motivo, string? detalleJson)
    {
        using var context = await _factory.CreateDbContextAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET QUOTED_IDENTIFIER ON; EXEC VEN.sp_ReversoTransaccion @IdVenta={0}, @IdUsuario={1}, @Motivo={2}, @DetalleJson={3}",
                idVenta, idUsuario, motivo, detalleJson ?? "[]");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al reversar la transacción: {ex.Message}");
        }
    }
}


