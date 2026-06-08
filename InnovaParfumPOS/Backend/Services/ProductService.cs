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
    Task<List<Producto>> GetFilteredProductsAsync(ProductFilterDto filter);
    Task<List<string>> GetDistinctMarcasAsync();
    Task<List<string>> GetDistinctGenerosAsync();
    Task<List<string>> GetDistinctOrigenesAsync();
    Task<Producto?> GetProductByIdAsync(int id);
    Task<Producto?> GetProductByCodeAsync(string code);
    Task<Producto?> GetProductByBarcodeAsync(string barcode);
    Task<bool> IsBarcodeUniqueAsync(string barcode);
    Task<List<Producto>> SearchProductsAsync(string term, bool onlyInStock = false);
    Task CreateProductAsync(Producto producto);
    Task UpdateProductAsync(Producto producto);
    Task<List<Categoria>> GetCategoriasAsync();
    Task<Categoria> AddCategoriaAsync(Categoria categoria);
    Task UpdateCategoriaAsync(Categoria categoria);
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

        public async Task<List<string>> GetDistinctMarcasAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Where(p => p.Marca != null && p.Marca != "")
            .Select(p => p.Marca.ToLower().Trim())
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctGenerosAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Where(p => p.Genero != null && p.Genero != "")
            .Select(p => p.Genero.ToLower().Trim())
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctOrigenesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Where(p => p.OrigenTipo != null && p.OrigenTipo != "")
            .Select(p => p.OrigenTipo.ToLower().Trim())
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Producto>> GetFilteredProductsAsync(ProductFilterDto filter)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var query = context.Productos
            .FromSqlRaw("SELECT * FROM INV.V_PRODUCTOS_DETALLE")
            .Include(p => p.IdCategoriaNavigation)
            .AsNoTracking();

        // 1. Estado de Actividad
        if (filter.ActivityStatus == "activos") query = query.Where(p => p.Activo == true);
        else if (filter.ActivityStatus == "inactivos") query = query.Where(p => p.Activo == false);
        // "todos" no filtra por Activo

        // 2. Categoria
        if (filter.IdCategoria.HasValue && filter.IdCategoria > 0)
            query = query.Where(p => p.IdCategoria == filter.IdCategoria);

        // 3. Estado de Stock
        if (filter.StockStatus == "con_stock") query = query.Where(p => p.StockActual > 0);
        else if (filter.StockStatus == "stock_bajo") query = query.Where(p => p.StockActual <= p.StockMinimo && p.StockActual > 0);
        else if (filter.StockStatus == "sin_stock") query = query.Where(p => p.StockActual == 0);

        // 4. Vencimiento
        if (filter.ShowExpiredOnly)
        {
            var today = DateTime.Today;
            query = query.Where(p => p.FechaVencimiento.HasValue && p.FechaVencimiento.Value < today);
        }
        else if (filter.ExpireInMonths.HasValue && filter.ExpireInMonths.Value > 0)
        {
            var today = DateTime.Today;
            var maxDate = today.AddMonths(filter.ExpireInMonths.Value);
            query = query.Where(p => p.FechaVencimiento.HasValue && p.FechaVencimiento.Value >= today && p.FechaVencimiento.Value <= maxDate);
        }

        // 5. Atributos dinámicos (Insensibles a mayúsculas/minúsculas)
        if (!string.IsNullOrWhiteSpace(filter.Marca))
        {
            var marca = filter.Marca.ToLower().Trim();
            query = query.Where(p => p.Marca != null && p.Marca.ToLower().Contains(marca));
        }

        if (!string.IsNullOrWhiteSpace(filter.Genero))
        {
            var genero = filter.Genero.ToLower().Trim();
            query = query.Where(p => p.Genero != null && p.Genero.ToLower().Contains(genero));
        }

        if (!string.IsNullOrWhiteSpace(filter.OrigenTipo))
        {
            var origen = filter.OrigenTipo.ToLower().Trim();
            query = query.Where(p => p.OrigenTipo != null && p.OrigenTipo.ToLower().Contains(origen));
        }

        // 6. Búsqueda por texto (Nombre o Código)
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.ToLower().Trim();
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

        public async Task<Categoria> AddCategoriaAsync(Categoria categoria)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    public async Task UpdateCategoriaAsync(Categoria categoria)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Categorias.Update(categoria);
        await context.SaveChangesAsync();
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


