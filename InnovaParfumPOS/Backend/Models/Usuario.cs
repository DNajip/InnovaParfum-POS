using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Username { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public byte[] PasswordSalt { get; set; } = null!;

    public int IdEmpleado { get; set; }

    public int IdRol { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public int IdEstado { get; set; }
    public int IntentosFallidos { get; set; }

    public virtual ICollection<Configuracion> Configuracions { get; set; } = new List<Configuracion>();

    public virtual Empleado IdEmpleadoNavigation { get; set; } = null!;

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Movimiento> Movimientos { get; set; } = new List<Movimiento>();

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();

    public virtual ICollection<ReclamosGarantium> ReclamosGarantia { get; set; } = new List<ReclamosGarantium>();

    public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public virtual ICollection<Venta> VentaIdUsuarioAnulaNavigations { get; set; } = new List<Venta>();

    public virtual ICollection<Venta> VentaIdUsuarioNavigations { get; set; } = new List<Venta>();

    public virtual ICollection<MovimientoVario> MovimientosVarios { get; set; } = new List<MovimientoVario>();

    public virtual ICollection<Modulo> IdModulos { get; set; } = new List<Modulo>();
}

