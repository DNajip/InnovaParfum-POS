using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaParfumPOS.Backend.Models;

[Table("CREDITOS", Schema = "VEN")]
public class Credito
{
    [Key]
    [Column("ID_CREDITO")]
    public int IdCredito { get; set; }

    [Column("ID_VENTA")]
    public int IdVenta { get; set; }

    [Column("ID_PERSONA")]
    public int IdPersona { get; set; }

    [Column("MONTO_ORIGINAL", TypeName = "decimal(18, 2)")]
    public decimal MontoOriginal { get; set; }

    [Column("SALDO_PENDIENTE", TypeName = "decimal(18, 2)")]
    public decimal SaldoPendiente { get; set; }

    [Column("FECHA_CREDITO", TypeName = "date")]
    public DateTime FechaCredito { get; set; }

    [Column("FECHA_VENCIMIENTO", TypeName = "date")]
    public DateTime FechaVencimiento { get; set; }

    [Column("ESTADO")]
    [MaxLength(20)]
    public string Estado { get; set; } = "ACTIVO";

    [ForeignKey("IdVenta")]
    public virtual Venta Venta { get; set; } = null!;

    [ForeignKey("IdPersona")]
    public virtual Persona Persona { get; set; } = null!;

    public virtual ICollection<CreditoAbono> Abonos { get; set; } = new List<CreditoAbono>();
}
