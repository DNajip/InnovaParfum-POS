using InnovaParfumPOS.Backend.Models;
using InnovaParfumPOS.Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnovaParfumPOS.Backend.Services;

public interface IClienteService
{
    Task<List<Persona>> GetAllClientesAsync(string? search = null);
    Task<Persona?> GetClienteByIdAsync(int id);
    Task CreateClienteAsync(Persona persona);
    Task UpdateClienteAsync(Persona persona);
    Task<List<Venta>> GetComprasClienteAsync(int idPersona);
    Task<List<Garantia>> GetGarantiasClienteAsync(int idPersona);
    Task<ClienteStatsDto> GetClienteStatsAsync();
    Task<List<TipoIdentificacion>> GetTiposIdentificacionAsync();
}

public class ClienteService : IClienteService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;

    public ClienteService(IDbContextFactory<InnovaParfumDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Persona>> GetAllClientesAsync(string? search = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.Personas
            .Include(p => p.IdTipo)
            .Include(p => p.Venta)
            .Where(p => p.EsCliente == true && p.IdEstado == 1);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Replace("-", "").Replace(" ", "").ToLower();
            search = search.ToLower();
            query = query.Where(p =>
                (p.NombreCompleto != null && p.NombreCompleto.ToLower().Contains(search)) ||
                p.NumIdentificacion.Contains(cleanSearch) ||
                (p.Telefono != null && p.Telefono.Contains(search)));
        }

        return await query
            .OrderByDescending(p => p.IdPersona)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Persona?> GetClienteByIdAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Personas
            .Include(p => p.IdTipo)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdPersona == id && p.EsCliente == true);
    }

    public async Task CreateClienteAsync(Persona persona)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // Normalización de identificación
        if (!string.IsNullOrWhiteSpace(persona.NumIdentificacion))
        {
            persona.NumIdentificacion = persona.NumIdentificacion.Replace("-", "").Replace(" ", "").ToUpper();
        }

        var exists = await context.Personas.AnyAsync(p => p.NumIdentificacion == persona.NumIdentificacion);
        if (exists)
            throw new Exception($"Ya existe una persona registrada con la identificación {persona.NumIdentificacion}.");

        persona.EsCliente = true;
        persona.EsEmpleado = false;
        persona.FechaCreacion = DateTime.Now;
        persona.IdEstado = 1; // Activo
        context.Personas.Add(persona);
        await context.SaveChangesAsync();
    }

    public async Task UpdateClienteAsync(Persona persona)
    {
        using var context = await _factory.CreateDbContextAsync();
        var existing = await context.Personas
            .FirstOrDefaultAsync(p => p.IdPersona == persona.IdPersona);
        if (existing == null)
            throw new Exception($"Cliente con ID {persona.IdPersona} no encontrado.");

        existing.PrimerNombre = persona.PrimerNombre;
        existing.SegundoNombre = persona.SegundoNombre;
        existing.PrimerApellido = persona.PrimerApellido;
        existing.SegundoApellido = persona.SegundoApellido;
        existing.IdTipoId = persona.IdTipoId;
        
        // Normalización de identificación
        if (!string.IsNullOrWhiteSpace(persona.NumIdentificacion))
        {
            existing.NumIdentificacion = persona.NumIdentificacion.Replace("-", "").Replace(" ", "").ToUpper();
        }
        else
        {
            existing.NumIdentificacion = persona.NumIdentificacion;
        }

        var exists = await context.Personas.AnyAsync(p => p.NumIdentificacion == existing.NumIdentificacion && p.IdPersona != existing.IdPersona);
        if (exists)
            throw new Exception($"Ya existe otra persona registrada con la identificación {existing.NumIdentificacion}.");

        existing.IdGenero = persona.IdGenero;
        existing.Telefono = persona.Telefono;
        existing.Email = persona.Email;
        existing.Direccion = persona.Direccion;

        await context.SaveChangesAsync();
    }

    public async Task<List<Venta>> GetComprasClienteAsync(int idPersona)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Ventas
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .Include(v => v.VentaDetalles)
                .ThenInclude(d => d.IdPeriodoGarantiaNavigation)
            .Include(v => v.IdUsuarioNavigation)
            .Where(v => v.IdPersona == idPersona && !v.Anulada)
            .OrderByDescending(v => v.FechaVenta)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Garantia>> GetGarantiasClienteAsync(int idPersona)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Garantias
            .Include(g => g.IdProductoNavigation)
            .Include(g => g.IdDetalleVentaNavigation)
            .Where(g => g.IdPersona == idPersona)
            .OrderByDescending(g => g.FechaInicio)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ClienteStatsDto> GetClienteStatsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // Usamos la vista de base de datos para obtener estadísticas precisas y centralizadas
        var stats = await context.VClienteDashboardStats.FirstOrDefaultAsync();

        if (stats == null) return new ClienteStatsDto();

        return new ClienteStatsDto
        {
            TotalClientes = stats.TotalClientes,
            ConGarantiasActivas = stats.TotalGarantiasActivas, // Ahora toma el total de garantías, no solo clientes distintos
            ConComprasRecientes = stats.ClientesConComprasRecientes
        };
    }

    public async Task<List<TipoIdentificacion>> GetTiposIdentificacionAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.TipoIdentificacions
            .Where(t => t.IdEstado == 1)
            .OrderBy(t => t.DescTipo)
            .AsNoTracking()
            .ToListAsync();
    }
}

