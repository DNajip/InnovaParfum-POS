using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaParfumPOS.Backend.Models;

[Table("CLIENTES_CREDITO", Schema = "ADM")]
public class ClienteCredito
{
    [Key]
    [Column("ID_CLIENTE_CREDITO")]
    public int IdClienteCredito { get; set; }

    [Column("ID_PERSONA")]
    public int IdPersona { get; set; }

    [Column("LIMITE_CREDITO", TypeName = "decimal(18, 2)")]
    public decimal LimiteCredito { get; set; }

    [Column("SALDO_ACTUAL", TypeName = "decimal(18, 2)")]
    public decimal SaldoActual { get; set; }

    [Column("DIAS_CREDITO")]
    public int DiasCredito { get; set; }

    [Column("ACTIVO")]
    public bool Activo { get; set; }

    [Column("FECHA_CREACION")]
    public DateTime FechaCreacion { get; set; }

    [ForeignKey("IdPersona")]
    public virtual Persona Persona { get; set; } = null!;
}
