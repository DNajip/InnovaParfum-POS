using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaParfumPOS.Backend.Models;

[Table("CREDITO_ABONOS", Schema = "VEN")]
public class CreditoAbono
{
    [Key]
    [Column("ID_ABONO")]
    public int IdAbono { get; set; }

    [Column("ID_CREDITO")]
    public int IdCredito { get; set; }

    [Column("MONTO", TypeName = "decimal(18, 2)")]
    public decimal Monto { get; set; }

    [Column("TASA_CAMBIO", TypeName = "decimal(12, 4)")]
    public decimal TasaCambio { get; set; }

    [Column("MONTO_RECIBIDO_MONEDA", TypeName = "decimal(18, 2)")]
    public decimal MontoRecibidoMoneda { get; set; }

    [Column("VUELTO_Base", TypeName = "decimal(18, 2)")]
    public decimal VueltoBase { get; set; }

    [Column("MONEDA_VUELTO")]
    [MaxLength(3)]
    public string? MonedaVuelto { get; set; }

    [Column("FECHA")]
    public DateTime Fecha { get; set; }

    [Column("ID_METODO_PAGO")]
    public int IdMetodoPago { get; set; }

    [Column("ID_USUARIO")]
    public int IdUsuario { get; set; }

    [Column("OBSERVACION")]
    [MaxLength(200)]
    public string? Observacion { get; set; }

    [ForeignKey("IdCredito")]
    public virtual Credito Credito { get; set; } = null!;

    [ForeignKey("IdMetodoPago")]
    public virtual MetodosPago MetodoPago { get; set; } = null!;

    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;
}
