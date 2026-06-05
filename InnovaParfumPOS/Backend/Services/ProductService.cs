using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using InnovaParfumPOS.Backend.Services;
using InnovaParfumPOS.Backend.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace InnovaParfumPOS.Backend.Services;

public interface IProductService
{
    Task<List<Producto>> GetAllProductsAsync(string? search = null, int? idCategoria = null, bool includeInactive = false, bool onlyInStock = false);
    Task<Producto?> GetProductByIdAsync(int id);
    Task<Producto?> GetProductByCodeAsync(string code);
    Task<Producto?> GetProductByBarcodeAsync(string barcode);
    Task<bool> IsBarcodeUniqueAsync(string barcode);
    Task<List<Producto>> SearchProductsAsync(string term, bool onlyInStock = false);
    Task CreateProductAsync(Producto producto);
    Task UpdateProductAsync(Producto producto);
    Task<List<Categoria>> GetCategoriasAsync();
    Task AdjustStockAsync(int idProducto, int nuevaCantidad, string observacion);
    Task<List<VStockCritico>> GetStockCriticoAsync();
    Task<InventoryStatsDto> GetInventoryStatsAsync(string? search = null, int? idCategoria = null);
    Task<List<Movimiento>> GetProductMovementsAsync(int idProducto);
}

public class ProductService : IProductService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;
    private readonly UserSession _userSession;

    public ProductService(IDbContextFactory<InnovaParfumDbContext> factory, UserSession userSession)
    {
        _factory = factory;
        _userSession = userSession;
    }

    public async Task<List<Producto>> GetAllProductsAsync(string? search = null, int? idCategoria = null, bool includeInactive = false, bool onlyInStock = false)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var query = context.Productos
            .FromSqlRaw("SELECT * FROM INV.V_PRODUCTOS_DETALLE")
            .Include(p => p.IdCategoriaNavigation)
            .AsNoTracking();

        if (!includeInactive)
            query = query.Where(p => p.Activo == true);

        if (idCategoria.HasValue && idCategoria > 0)
            query = query.Where(p => p.IdCategoria == idCategoria);

        if (onlyInStock)
            query = query.Where(p => p.StockActual > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(s) || (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(s)));
        }

        return await query
            .OrderByDescending(p => p.IdProducto)
            .ToListAsync();
    }

    public async Task<Producto?> GetProductByIdAsync(int id)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Include(p => p.IdCategoriaNavigation)
            .FirstOrDefaultAsync(p => p.IdProducto == id);
    }

    public async Task<Producto?> GetProductByCodeAsync(string code)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Include(p => p.IdCategoriaNavigation)
            .FirstOrDefaultAsync(p => p.CodigoBarras == code && p.Activo == true);
    }

    public async Task<Producto?> GetProductByBarcodeAsync(string barcode) => await GetProductByCodeAsync(barcode);

    public async Task<bool> IsBarcodeUniqueAsync(string barcode)
    {
        using var context = await _factory.CreateDbContextAsync();
        return !await context.Productos.AnyAsync(p => p.CodigoBarras == barcode);
    }

    public async Task<List<Producto>> SearchProductsAsync(string term, bool onlyInStock = false) => await GetAllProductsAsync(search: term, onlyInStock: onlyInStock);

    public async Task CreateProductAsync(Producto producto)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Session = _userSession;
        
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INV.sp_MantenerProducto";
        command.CommandType = CommandType.StoredProcedure;

        AddParam(command, "@IdProducto", DBNull.Value);
        AddParam(command, "@CodigoBarras", (object?)producto.CodigoBarras ?? DBNull.Value);
        AddParam(command, "@Nombre", producto.Nombre);
        AddParam(command, "@Marca", (object?)producto.Marca ?? DBNull.Value);
        AddParam(command, "@Genero", (object?)producto.Genero ?? DBNull.Value);
        AddParam(command, "@OrigenTipo", (object?)producto.OrigenTipo ?? DBNull.Value);
        AddParam(command, "@Concentracion", (object?)producto.Concentracion ?? DBNull.Value);
        AddParam(command, "@FechaVencimiento", (object?)producto.FechaVencimiento ?? DBNull.Value);
        AddParam(command, "@IdCategoria", (object?)producto.IdCategoria ?? DBNull.Value);
        AddParam(command, "@TipoProducto", producto.TipoProducto);
        AddParam(command, "@PrecioCompra", producto.PrecioCompra);
        AddParam(command, "@PrecioVenta", producto.PrecioVenta);
        AddParam(command, "@StockActual", producto.StockActual);
        AddParam(command, "@StockMinimo", producto.StockMinimo);
        AddParam(command, "@Activo", producto.Activo);
        
        // UserId ya es int?, no requiere parseo
        var userId = (object?)_userSession.UserId ?? DBNull.Value;
        AddParam(command, "@UsuarioId", userId);

        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateProductAsync(Producto producto)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Session = _userSession;

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INV.sp_MantenerProducto";
        command.CommandType = CommandType.StoredProcedure;

        AddParam(command, "@IdProducto", producto.IdProducto);
        AddParam(command, "@CodigoBarras", (object?)producto.CodigoBarras ?? DBNull.Value);
        AddParam(command, "@Nombre", producto.Nombre);
        AddParam(command, "@Marca", (object?)producto.Marca ?? DBNull.Value);
        AddParam(command, "@Genero", (object?)producto.Genero ?? DBNull.Value);
        AddParam(command, "@OrigenTipo", (object?)producto.OrigenTipo ?? DBNull.Value);
        AddParam(command, "@Concentracion", (object?)producto.Concentracion ?? DBNull.Value);
        AddParam(command, "@FechaVencimiento", (object?)producto.FechaVencimiento ?? DBNull.Value);
        AddParam(command, "@IdCategoria", (object?)producto.IdCategoria ?? DBNull.Value);
        AddParam(command, "@TipoProducto", producto.TipoProducto);
        AddParam(command, "@PrecioCompra", producto.PrecioCompra);
        AddParam(command, "@PrecioVenta", producto.PrecioVenta);
        AddParam(command, "@StockActual", producto.StockActual);
        AddParam(command, "@StockMinimo", producto.StockMinimo);
        AddParam(command, "@Activo", producto.Activo);
        
        var userId = (object?)_userSession.UserId ?? DBNull.Value;
        AddParam(command, "@UsuarioId", userId);

        await command.ExecuteNonQueryAsync();
    }

    private void AddParam(DbCommand command, string name, object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        command.Parameters.Add(param);
    }

    public async Task<List<Categoria>> GetCategoriasAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Categorias
            .Where(c => c.IdEstado == 1)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task AdjustStockAsync(int idProducto, int nuevaCantidad, string observacion)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Session = _userSession;
        var producto = await context.Productos.FindAsync(idProducto);
        if (producto == null) return;

        producto.StockActual = nuevaCantidad;
        await context.SaveChangesAsync();
    }

    public async Task<List<VStockCritico>> GetStockCriticoAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.VStockCriticos.ToListAsync();
    }

    public async Task<InventoryStatsDto> GetInventoryStatsAsync(string? search = null, int? idCategoria = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var query = context.Productos
            .Where(p => p.Activo == true)
            .AsNoTracking();

        if (idCategoria.HasValue && idCategoria > 0)
            query = query.Where(p => p.IdCategoria == idCategoria);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(s) || (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(s)));
        }

        var productos = await query.ToListAsync();

        return new InventoryStatsDto
        {
            TotalProductos = productos.Count,
            StockBajo = productos.Count(p => p.StockActual > 0 && p.StockActual <= p.StockMinimo),
            SinStock = productos.Count(p => p.StockActual == 0),
            Valorizacion = productos.Sum(p => p.PrecioVenta * p.StockActual)
        };
    }

    public async Task<List<Movimiento>> GetProductMovementsAsync(int idProducto)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Movimientos
            .Include(m => m.IdTipoMovNavigation)
            .Include(m => m.RegistradoPorNavigation)
            .Where(m => m.IdProducto == idProducto)
            .OrderByDescending(m => m.FechaMov)
            .ToListAsync();
    }
}

