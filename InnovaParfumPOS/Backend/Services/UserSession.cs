using InnovaParfumPOS.Backend.Models;

namespace InnovaParfumPOS.Backend.Services;

public class UserSession
{
    public event Action? OnChange;

    private int? _userId;
    public int? UserId 
    { 
        get => _userId; 
        set 
        {
            if (_userId != value)
            {
                _userId = value;
                NotifyStateChanged();
            }
        }
    }
    
    public string? Username { get; set; }
    public string? NombreCompleto { get; set; }
    public string? Rol { get; set; }
    
    private Turno? _activeShift;
    public Turno? ActiveShift
    {
        get => _activeShift;
        set
        {
            if (_activeShift != value)
            {
                _activeShift = value;
                NotifyStateChanged();
            }
        }
    }

    public bool IsCashOpen => ActiveShift != null;

    public string? CurrentObservation { get; set; }

    public bool IsAuthenticated => _userId.HasValue;
    
    public List<string> PermittedModules { get; set; } = new();

    public bool HasModuleAccess(string moduleName)
    {
        // Si no hay módulos cargados (ej. admin por defecto), se asume acceso total por simplicidad en desarrollo
        // Para producción, esto debería ser más estricto
        if (!PermittedModules.Any() && Rol == "ADMINISTRADOR") return true;
        
        return PermittedModules.Contains(moduleName, StringComparer.OrdinalIgnoreCase);
    }

    public void Clear()
    {
        UserId = null;
        Username = null;
        NombreCompleto = null;
        Rol = null;
        CurrentObservation = null;
        ActiveShift = null;
        PermittedModules.Clear();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

