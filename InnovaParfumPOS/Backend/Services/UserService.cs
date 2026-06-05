using InnovaParfumPOS.Backend.Models;
using InnovaParfumPOS.Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace InnovaParfumPOS.Backend.Services;

public class UserService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;
    private readonly UserSession _session;

    public UserService(IDbContextFactory<InnovaParfumDbContext> factory, UserSession session)
    {
        _factory = factory;
        _session = session;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Usuarios
            .Include(u => u.IdEmpleadoNavigation)
                .ThenInclude(e => e.IdPersonaNavigation)
            .Include(u => u.IdRolNavigation)
            .Include(u => u.IdModulos)
            .Select(u => new UserDto
            {
                IdUsuario = u.IdUsuario,
                IdEmpleado = u.IdEmpleado,
                IdPersona = u.IdEmpleadoNavigation.IdPersona,
                Username = u.Username,
                IdRol = u.IdRol,
                NombreRol = u.IdRolNavigation.Nombre,
                IdEstado = u.IdEstado,
                PrimerNombre = u.IdEmpleadoNavigation.IdPersonaNavigation.PrimerNombre,
                SegundoNombre = u.IdEmpleadoNavigation.IdPersonaNavigation.SegundoNombre,
                PrimerApellido = u.IdEmpleadoNavigation.IdPersonaNavigation.PrimerApellido,
                SegundoApellido = u.IdEmpleadoNavigation.IdPersonaNavigation.SegundoApellido,
                IdTipoId = u.IdEmpleadoNavigation.IdPersonaNavigation.IdTipoId,
                NumIdentificacion = u.IdEmpleadoNavigation.IdPersonaNavigation.NumIdentificacion,
                IdGenero = u.IdEmpleadoNavigation.IdPersonaNavigation.IdGenero,
                Telefono = u.IdEmpleadoNavigation.IdPersonaNavigation.Telefono,
                Email = u.IdEmpleadoNavigation.IdPersonaNavigation.Email,
                Direccion = u.IdEmpleadoNavigation.IdPersonaNavigation.Direccion,
                SelectedModuloIds = u.IdModulos.Select(m => m.IdModulo).ToList()
            })
            .AsNoTracking()
            .OrderBy(u => u.PrimerNombre)
            .ToListAsync();
    }

    public async Task<List<Role>> GetRolesAsync() 
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Roles.Where(r => r.IdEstado == 1).ToListAsync();
    }

    public async Task<List<TipoIdentificacion>> GetTiposIdentificacionAsync() 
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.TipoIdentificacions.Where(t => t.IdEstado == 1).ToListAsync();
    }

    public async Task<List<Genero>> GetGenerosAsync() 
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Generos.Where(g => g.IdEstado == 1).ToListAsync();
    }

    public async Task<List<Modulo>> GetModulosAsync() 
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Modulos.Where(m => m.IdEstado == 1).OrderBy(m => m.Orden).ToListAsync();
    }

    public async Task CreateUserAsync(UserDto dto)
    {
        using var context = await _factory.CreateDbContextAsync();
        // 0. Normalizar identificacion y username
        dto.Username = dto.Username.Replace("-", "").Replace(" ", "");
        dto.NumIdentificacion = dto.NumIdentificacion.Replace("-", "").Replace(" ", "");

        // 1. Validar Username
        if (await context.Usuarios.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
        {
            throw new Exception("El nombre de usuario ya está en uso.");
        }

        // 2. Buscar o Crear Persona
        var persona = await context.Personas
            .FirstOrDefaultAsync(p => p.NumIdentificacion == dto.NumIdentificacion);

        if (persona == null)
        {
            persona = new Persona
            {
                PrimerNombre = dto.PrimerNombre,
                SegundoNombre = dto.SegundoNombre,
                PrimerApellido = dto.PrimerApellido,
                SegundoApellido = dto.SegundoApellido,
                IdTipoId = dto.IdTipoId,
                NumIdentificacion = dto.NumIdentificacion,
                IdGenero = dto.IdGenero,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Direccion = dto.Direccion,
                EsCliente = false,
                EsEmpleado = true,
                IdEstado = 1,
                NombreCompleto = $"{dto.PrimerNombre} {dto.PrimerApellido}".Trim(),
                FechaCreacion = DateTime.Now
            };
            context.Personas.Add(persona);
            await context.SaveChangesAsync();
        }
        else
        {
            // Si ya existe la persona, nos aseguramos que esté marcada como empleado
            if (persona.EsEmpleado != true)
            {
                persona.EsEmpleado = true;
                await context.SaveChangesAsync();
            }
        }

        // 3. Buscar o Crear Empleado
        var empleado = await context.Empleados
            .FirstOrDefaultAsync(e => e.IdPersona == persona.IdPersona);

        if (empleado == null)
        {
            empleado = new Empleado
            {
                IdPersona = persona.IdPersona,
                IdRol = dto.IdRol,
                IdEstado = 1,
                FechaContratacion = DateOnly.FromDateTime(DateTime.Now)
            };
            context.Empleados.Add(empleado);
            await context.SaveChangesAsync();
        }
        else
        {
            // Validar que el empleado no tenga ya un usuario asignado
            if (await context.Usuarios.AnyAsync(u => u.IdEmpleado == empleado.IdEmpleado))
            {
                throw new Exception($"La persona con identificación {dto.NumIdentificacion} ya tiene un usuario de sistema asignado.");
            }
        }

        // 4. Crear Usuario
        var (passwordHash, passwordSalt) = await CreatePasswordHashAsync(dto.Password ?? "123456");

        var usuario = new Usuario
        {
            IdEmpleado = empleado.IdEmpleado,
            Username = dto.Username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            IdRol = dto.IdRol,
            IdEstado = 1,
            FechaCreacion = DateTime.Now
        };
        
        if (dto.SelectedModuloIds != null && dto.SelectedModuloIds.Any())
        {
            var modulos = await context.Modulos.Where(m => dto.SelectedModuloIds.Contains(m.IdModulo)).ToListAsync();
            foreach (var mod in modulos)
            {
                usuario.IdModulos.Add(mod);
            }
        }

        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(UserDto dto)
    {
        using var context = await _factory.CreateDbContextAsync();
        context.Session = _session; // Pasar la sesión para auditoría SQL
        
        if (dto.IdUsuario == null) throw new Exception("ID de usuario no válido.");

        var usuario = await context.Usuarios
            .Include(u => u.IdEmpleadoNavigation)
                .ThenInclude(e => e.IdPersonaNavigation)
            .Include(u => u.IdModulos)
            .FirstOrDefaultAsync(u => u.IdUsuario == dto.IdUsuario);

        if (usuario == null) throw new Exception("Usuario no encontrado.");

        try 
        {
            // Normalizar entrada
            dto.Username = dto.Username?.Replace("-", "").Replace(" ", "") ?? "";
            dto.NumIdentificacion = dto.NumIdentificacion?.Replace("-", "").Replace(" ", "") ?? "";

            // Validar Username único si lo cambió
            if (usuario.Username.ToLower() != dto.Username.ToLower() && 
                await context.Usuarios.AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower()))
            {
                throw new Exception("El nombre de usuario ya está en uso por otra cuenta.");
            }

            // Actualizar Persona
            var persona = usuario.IdEmpleadoNavigation.IdPersonaNavigation;
            persona.PrimerNombre = dto.PrimerNombre;
            persona.SegundoNombre = dto.SegundoNombre;
            persona.PrimerApellido = dto.PrimerApellido;
            persona.SegundoApellido = dto.SegundoApellido;
            persona.NombreCompleto = $"{dto.PrimerNombre} {dto.PrimerApellido}".Trim();
            persona.IdTipoId = dto.IdTipoId;
            persona.NumIdentificacion = dto.NumIdentificacion;
            persona.IdGenero = dto.IdGenero;
            persona.Telefono = dto.Telefono;
            persona.Email = dto.Email;
            persona.Direccion = dto.Direccion;

            // Actualizar Empleado y Usuario (Rol redundante en ambas tablas)
            usuario.IdEmpleadoNavigation.IdRol = dto.IdRol;
            usuario.IdRol = dto.IdRol;
            usuario.Username = dto.Username;

            // Actualizar contraseña si se proporcionó una nueva
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                var (passwordHash, passwordSalt) = await CreatePasswordHashAsync(dto.Password);
                usuario.PasswordHash = passwordHash;
                usuario.PasswordSalt = passwordSalt;
            }

            // Actualizar Módulos de forma quirúrgica
            var currentModIds = usuario.IdModulos.Select(m => m.IdModulo).ToList();
            var targetModIds = dto.SelectedModuloIds ?? new List<int>();

            // Quitar los que ya no están
            var toRemove = usuario.IdModulos.Where(m => !targetModIds.Contains(m.IdModulo)).ToList();
            foreach (var mod in toRemove) usuario.IdModulos.Remove(mod);

            // Agregar los nuevos
            var toAddIds = targetModIds.Where(id => !currentModIds.Contains(id)).ToList();
            if (toAddIds.Any())
            {
                var newModulos = await context.Modulos.Where(m => toAddIds.Contains(m.IdModulo)).ToListAsync();
                foreach (var mod in newModulos) usuario.IdModulos.Add(mod);
            }

            await context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
            throw new Exception($"Error de base de datos al guardar: {innerMsg}");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al actualizar usuario: {ex.Message}");
        }
    }

    public async Task ToggleUserStatusAsync(int idUsuario)
    {
        using var context = await _factory.CreateDbContextAsync();
        var usuario = await context.Usuarios.FindAsync(idUsuario);
        if (usuario != null)
        {
            if (usuario.IdEstado == 3) // 3 = Bloqueado
            {
                usuario.IdEstado = 1; // Restaurar a Activo
                usuario.IntentosFallidos = 0; // Reiniciar contador
            }
            else
            {
                usuario.IdEstado = usuario.IdEstado == 1 ? 2 : 1; // Alternar entre Activo (1) e Inactivo (2)
            }
            await context.SaveChangesAsync();
        }
    }

    private async Task<(byte[] Hash, byte[] Salt)> CreatePasswordHashAsync(string password)
    {
        using var context = await _factory.CreateDbContextAsync();
        byte[] salt = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var connection = context.Database.GetDbConnection();
        bool connectionOpened = false;
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
            connectionOpened = true;
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT HASHBYTES('SHA2_512', @Password + CAST(@Salt AS NVARCHAR(MAX)))";

            var paramPassword = command.CreateParameter();
            paramPassword.ParameterName = "@Password";
            paramPassword.Value = password;
            command.Parameters.Add(paramPassword);

            var paramSalt = command.CreateParameter();
            paramSalt.ParameterName = "@Salt";
            paramSalt.Value = salt;
            paramSalt.DbType = System.Data.DbType.Binary;
            command.Parameters.Add(paramSalt);

            var hashResult = await command.ExecuteScalarAsync();
            byte[] hash = (byte[])hashResult!;
            
            return (hash, salt);
        }
        finally
        {
            if (connectionOpened)
            {
                await connection.CloseAsync();
            }
        }
    }
}

