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
    Task<List<Genero>> GetGenerosAsync();
    Task<List<Origen>> GetOrigenesAsync();
    Task<List<Concentracion>> GetConcentracionesAsync();
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
            .Include(p => p.IdOrigenNavigation)
            .Include(p => p.IdConcentracionNavigation)
            .Include(p => p.IdGeneroNavigation)
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
            .Select(p => p.Marca!.ToLower().Trim())
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Genero>> GetGenerosAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Generos.Where(g => g.IdEstado == 1).OrderBy(g => g.DescGenero).ToListAsync();
    }

    public async Task<List<Origen>> GetOrigenesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Origenes.Where(o => o.IdEstado == 1).OrderBy(o => o.Nombre).ToListAsync();
    }

    public async Task<List<Concentracion>> GetConcentracionesAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Concentraciones.Where(c => c.IdEstado == 1).OrderBy(c => c.Nombre).ToListAsync();
    }

    public async Task<List<Producto>> GetFilteredProductsAsync(ProductFilterDto filter)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        var query = context.Productos
            .FromSqlRaw("SELECT * FROM INV.V_PRODUCTOS_DETALLE")
            .Include(p => p.IdCategoriaNavigation)
            .Include(p => p.IdOrigenNavigation)
            .Include(p => p.IdConcentracionNavigation)
            .Include(p => p.IdGeneroNavigation)
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

        // 5. Atributos dinÃ¯Â¿Â½micos (Insensibles a mayÃ¯Â¿Â½sculas/minÃ¯Â¿Â½sculas)
        if (!string.IsNullOrWhiteSpace(filter.Marca))
        {
            var marca = filter.Marca.ToLower().Trim();
            query = query.Where(p => p.Marca != null && p.Marca!.ToLower().Contains(marca));
        }

        if (filter.IdGenero.HasValue && filter.IdGenero > 0)
        {
            
            query = query.Where(p => p.IdGenero == filter.IdGenero);
        }

        if (filter.IdOrigen.HasValue && filter.IdOrigen > 0)
        {
            
            query = query.Where(p => p.IdOrigen == filter.IdOrigen);
        }

        if (filter.Ml.HasValue)
        {
            query = query.Where(p => p.Ml == filter.Ml.Value);
        }

        // 6. BÃ¯Â¿Â½squeda por texto (Nombre o CÃ¯Â¿Â½digo)
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
            .Include(p => p.IdOrigenNavigation)
            .Include(p => p.IdConcentracionNavigation)
            .Include(p => p.IdGeneroNavigation)
            .FirstOrDefaultAsync(p => p.IdProducto == id);
    }

    public async Task<Producto?> GetProductByCodeAsync(string code)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Productos
            .Include(p => p.IdCategoriaNavigation)
            .Include(p => p.IdOrigenNavigation)
            .Include(p => p.IdConcentracionNavigation)
            .Include(p => p.IdGeneroNavigation)
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
        AddParam(command, "@IdGenero", (object?)producto.IdGenero ?? DBNull.Value);
        AddParam(command, "@IdOrigen", (object?)producto.IdOrigen ?? DBNull.Value);
        AddParam(command, "@IdConcentracion", (object?)producto.IdConcentracion ?? DBNull.Value);
        AddParam(command, "@Ml", (object?)producto.Ml ?? DBNull.Value);
        AddParam(command, "@FechaVencimiento", (object?)producto.FechaVencimiento ?? DBNull.Value);
        AddParam(command, "@IdCategoria", (object?)producto.IdCategoria ?? DBNull.Value);
        AddParam(command, "@TipoProducto", producto.TipoProducto);
        AddParam(command, "@CostoProducto", (object?)producto.CostoProducto ?? DBNull.Value);
          AddParam(command, "@CostoEnvio", (object?)producto.CostoEnvio ?? DBNull.Value);
          AddParam(command, "@PorcGananciaMayorista", (object?)producto.PorcGananciaMayorista ?? DBNull.Value);
          AddParam(command, "@PrecioMayorista", (object?)producto.PrecioMayorista ?? DBNull.Value);
          AddParam(command, "@PorcGananciaMinorista", (object?)producto.PorcGananciaMinorista ?? DBNull.Value);
          AddParam(command, "@PrecioMinorista", (object?)producto.PrecioMinorista ?? DBNull.Value);
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
        AddParam(command, "@IdGenero", (object?)producto.IdGenero ?? DBNull.Value);
        AddParam(command, "@IdOrigen", (object?)producto.IdOrigen ?? DBNull.Value);
        AddParam(command, "@IdConcentracion", (object?)producto.IdConcentracion ?? DBNull.Value);
        AddParam(command, "@Ml", (object?)producto.Ml ?? DBNull.Value);
        AddParam(command, "@FechaVencimiento", (object?)producto.FechaVencimiento ?? DBNull.Value);
        AddParam(command, "@IdCategoria", (object?)producto.IdCategoria ?? DBNull.Value);
        AddParam(command, "@TipoProducto", producto.TipoProducto);
        AddParam(command, "@CostoProducto", (object?)producto.CostoProducto ?? DBNull.Value);
          AddParam(command, "@CostoEnvio", (object?)producto.CostoEnvio ?? DBNull.Value);
          AddParam(command, "@PorcGananciaMayorista", (object?)producto.PorcGananciaMayorista ?? DBNull.Value);
          AddParam(command, "@PrecioMayorista", (object?)producto.PrecioMayorista ?? DBNull.Value);
          AddParam(command, "@PorcGananciaMinorista", (object?)producto.PorcGananciaMinorista ?? DBNull.Value);
          AddParam(command, "@PrecioMinorista", (object?)producto.PrecioMinorista ?? DBNull.Value);
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
            TotalCantidades = productos.Sum(p => p.StockActual),
            StockBajo = productos.Count(p => p.StockActual > 0 && p.StockActual <= p.StockMinimo),
            SinStock = productos.Count(p => p.StockActual == 0),
            ValorizacionMayorista = productos.Sum(p => (p.PrecioMayorista ?? 0) * p.StockActual),
            ValorizacionMinorista = productos.Sum(p => (p.PrecioMinorista ?? 0) * p.StockActual)
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













