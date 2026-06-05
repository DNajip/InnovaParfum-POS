using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Estado
{
    public int IdEstado { get; set; }

    public string Codigo { get; set; } = null!;

    public string DescEstado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual ICollection<Categoria> Categoria { get; set; } = new List<Categoria>();

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<Genero> Generos { get; set; } = new List<Genero>();

    public virtual ICollection<Modulo> Modulos { get; set; } = new List<Modulo>();

    public virtual ICollection<PeriodosGarantium> PeriodosGarantia { get; set; } = new List<PeriodosGarantium>();

    public virtual ICollection<Persona> Personas { get; set; } = new List<Persona>();

    public virtual ICollection<ReclamosGarantium> ReclamosGarantia { get; set; } = new List<ReclamosGarantium>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<TipoIdentificacion> TipoIdentificacions { get; set; } = new List<TipoIdentificacion>();

    public virtual ICollection<Turno> Turnos { get; set; } = new List<Turno>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

