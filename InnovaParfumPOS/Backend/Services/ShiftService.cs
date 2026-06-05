using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace InnovaParfumPOS.Backend.Services;

public interface IShiftService
{
    Task<Turno?> GetActiveShiftAsync(int userId);
    Task<Turno> OpenShiftAsync(int userId, decimal initialNio, decimal initialUsd, List<ConteoDenominacione> counts);
    Task<Turno> CloseShiftAsync(int turnoId, decimal finalNio, decimal finalUsd, List<ConteoDenominacione> counts, string? observations);
    Task<List<Denominacione>> GetDenominationsAsync();
    Task<MovimientoVario> AddCashEntryAsync(int turnoId, int userId, int monedaId, decimal monto, string concepto);
    Task<decimal> GetTotalCashEntriesAsync(int turnoId, int monedaId);
    Task<Turno?> GetLastClosedShiftAsync(int userId);
}

public class ShiftService : IShiftService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;

    public ShiftService(IDbContextFactory<InnovaParfumDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<Turno?> GetActiveShiftAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        // Usamos la vista para obtener el turno activo con datos calculados
        return await context.Turnos
            .FromSqlRaw("SELECT * FROM CAJA.V_ESTADO_TURNO_ACTUAL WHERE ID_USUARIO = {0}", userId)
            .Include(t => t.IdUsuarioNavigation)
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<Turno> OpenShiftAsync(int userId, decimal initialNio, decimal initialUsd, List<ConteoDenominacione> counts)
    {
        using var context = await _factory.CreateDbContextAsync();
        var countsJson = System.Text.Json.JsonSerializer.Serialize(counts);

        var result = await context.Turnos
            .FromSqlRaw("EXEC CAJA.sp_GestionarTurno @Accion='ABRIR', @IdUsuario={0}, @MontoInicialNio={1}, @MontoInicialUsd={2}, @ConteosJson={3}",
                userId, initialNio, initialUsd, countsJson)
            .AsNoTracking()
            .ToListAsync();

        if (!result.Any())
            throw new Exception("No se pudo abrir el turno en la base de datos.");

        return result.First();
    }

    public async Task<List<Denominacione>> GetDenominationsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Denominaciones
            .Include(d => d.IdMonedaNavigation)
            .OrderBy(d => d.IdMoneda)
            .ThenByDescending(d => d.Orden)
            .ToListAsync();
    }

    public async Task<Turno> CloseShiftAsync(int turnoId, decimal finalNio, decimal finalUsd, List<ConteoDenominacione> counts, string? observations)
    {
        using var context = await _factory.CreateDbContextAsync();
        var countsJson = System.Text.Json.JsonSerializer.Serialize(counts);

        var result = await context.Turnos
            .FromSqlRaw("EXEC CAJA.sp_GestionarTurno @Accion='CERRAR', @IdUsuario=0, @IdTurno={0}, @MontoFinalNio={1}, @MontoFinalUsd={2}, @Observaciones={3}, @ConteosJson={4}",
                turnoId, finalNio, finalUsd, observations ?? (object)DBNull.Value, countsJson)
            .AsNoTracking()
            .ToListAsync();

        if (!result.Any())
            throw new Exception("No se pudo cerrar el turno en la base de datos.");

        return result.First();
    }

    public async Task<MovimientoVario> AddCashEntryAsync(int turnoId, int userId, int monedaId, decimal monto, string concepto)
    {
        using var context = await _factory.CreateDbContextAsync();
        var mov = new MovimientoVario
        {
            IdTurno = turnoId,
            IdUsuario = userId,
            IdMoneda = monedaId,
            Monto = monto,
            Concepto = concepto,
            Tipo = "INGRESO",
            Fecha = DateTime.Now
        };

        context.MovimientosVarios.Add(mov);
        await context.SaveChangesAsync();
        return mov;
    }

    public async Task<decimal> GetTotalCashEntriesAsync(int turnoId, int monedaId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.MovimientosVarios
            .Where(m => m.IdTurno == turnoId && m.IdMoneda == monedaId && m.Tipo == "INGRESO")
            .SumAsync(m => m.Monto);
    }

    public async Task<Turno?> GetLastClosedShiftAsync(int userId)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Turnos
            .Include(t => t.MovimientosVarios)
            .Where(t => t.IdUsuario == userId && t.FechaCierre != null)
            .OrderByDescending(t => t.FechaCierre)
            .FirstOrDefaultAsync();
    }
}

