using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class Persona
{
    public int IdPersona { get; set; }

    public string PrimerNombre { get; set; } = null!;

    public string? SegundoNombre { get; set; }

    public string PrimerApellido { get; set; } = null!;

    public string? SegundoApellido { get; set; }

    public int IdTipoId { get; set; }

    public string NumIdentificacion { get; set; } = null!;

    public int? IdGenero { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public bool EsCliente { get; set; }

    public bool EsEmpleado { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int IdEstado { get; set; }

    public string? NombreCompleto { get; set; }

    public virtual Empleado? Empleado { get; set; }

    public virtual ICollection<Garantia> Garantia { get; set; } = new List<Garantia>();

    public virtual Estado IdEstadoNavigation { get; set; } = null!;

    public virtual Genero? IdGeneroNavigation { get; set; }

    public virtual TipoIdentificacion IdTipo { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}

