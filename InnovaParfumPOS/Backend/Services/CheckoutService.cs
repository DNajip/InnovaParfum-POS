using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovaParfumPOS.Backend.Services;

public interface ICheckoutService
{
    Task<Venta> ProcessCheckoutAsync(int userId, int? idPersona, decimal discount, List<CartItem> items, List<PaymentInput> payments);
    Task<List<PeriodosGarantium>> GetPeriodosGarantiaAsync();
    Task<List<MetodosPago>> GetMetodosPagoAsync();
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

    public async Task<Venta> ProcessCheckoutAsync(int userId, int? idPersona, decimal discount, List<CartItem> items, List<PaymentInput> payments)
    {
        using var context = await _factory.CreateDbContextAsync();
        // 1. Serializar colecciones a JSON para enviarlas al SP
        var itemsJson = System.Text.Json.JsonSerializer.Serialize(items);
        
        // Mapeamos pagos para asegurar que las propiedades coincidan con el SP
        var paymentsMapped = payments.Select(p => new {
            p.IdMetodoPago,
            p.Monto,
            p.TasaCambio,
            MontoEnNio = p.MontoEnNio, // El monto bruto recibido en moneda nacional, calculado por el cliente
            p.Referencia
        });
        var paymentsJson = System.Text.Json.JsonSerializer.Serialize(paymentsMapped);

        try 
        {
            // 2. Ejecutar el SP Maestro de Venta
            var result = await context.Ventas
                .FromSqlRaw("EXEC VEN.sp_ProcesarVenta @IdUsuario={0}, @IdPersona={1}, @DescuentoNio={2}, @TasaCambioUsd={3}, @ItemsJson={4}, @PaymentsJson={5}",
                    userId,
                    idPersona ?? (object)DBNull.Value,
                    discount,
                    36.60m, // Podría venir de configuración
                    itemsJson,
                    paymentsJson)
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
}

