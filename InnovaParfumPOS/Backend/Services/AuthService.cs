using InnovaParfumPOS.Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace InnovaParfumPOS.Backend.Services;

public interface IAuthService
{
    Task<LoginResponse> IniciarSesionAsync(string username, string password);
}

public class LoginResponse
{
    public int Success { get; set; } // 1 = Éxito, 0 = Fallo (Coincide con el INT del SP)
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
}

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;
    private readonly UserSession _userSession;

    public AuthService(IDbContextFactory<InnovaParfumDbContext> factory, UserSession userSession)
    {
        _factory = factory;
        _userSession = userSession;
    }

    public async Task<LoginResponse> IniciarSesionAsync(string username, string password)
    {
        using var context = await _factory.CreateDbContextAsync();
        try 
        {
            var result = await context.Database
                .SqlQueryRaw<LoginResponse>("EXEC ADM.sp_IniciarSesion @Username={0}, @Password={1}", username, password)
                .ToListAsync();

            var response = result.FirstOrDefault() ?? new LoginResponse { Success = 0, Message = "Error inesperado en el servidor" };

            if (response.Success == 1 && response.UserId.HasValue)
            {
                var user = await context.Usuarios
                    .Include(u => u.IdEmpleadoNavigation)
                        .ThenInclude(e => e.IdPersonaNavigation)
                    .Include(u => u.IdRolNavigation)
                    .Include(u => u.IdModulos)
                    .FirstOrDefaultAsync(u => u.IdUsuario == response.UserId.Value);

                if (user != null)
                {
                    _userSession.UserId = user.IdUsuario;
                    _userSession.Username = user.Username;
                    _userSession.NombreCompleto = user.IdEmpleadoNavigation.IdPersonaNavigation.NombreCompleto;
                    _userSession.Rol = user.IdRolNavigation.Nombre;
                    _userSession.PermittedModules = user.IdModulos.Select(m => m.Nombre).ToList();
                    
                    return response;
                }
            }

            // Seguridad: Mensaje genérico para evitar enumeración de usuarios,
            // pero permitimos informar si la cuenta ha sido bloqueada.
            string finalMessage = response.Message == "Su cuenta ha sido bloqueada" 
                ? response.Message 
                : "Usuario o contraseña incorrectos";

            return new LoginResponse { Success = 0, Message = finalMessage };
        }
        catch (Exception ex)
        {
            return new LoginResponse { Success = 0, Message = $"Error de conexión: {ex.Message}" };
        }
    }
}

