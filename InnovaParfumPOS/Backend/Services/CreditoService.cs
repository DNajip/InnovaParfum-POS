using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public interface ICreditoService
{
    Task<ClienteCredito?> GetPerfilCreditoAsync(int idPersona);
    Task<ClienteCredito> AsignarLimiteCreditoAsync(int idPersona, decimal limite, int dias);
    Task<CreditoAbono> RegistrarAbonoAsync(int idCredito, int idUsuario, int idMetodoPago, decimal montoAbonoBase, decimal montoRecibidoMoneda, decimal tasaCambio, decimal vueltoBase, string observacion, string monedaVuelto = "NIO");
    Task<List<Credito>> GetCreditosActivosAsync(int idPersona);
    Task<List<Credito>> GetAllActiveCreditosAsync();
    Task<List<CreditoAbono>> GetHistorialAbonosAsync(int idCredito);
}

public class CreditoService : ICreditoService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;

    public CreditoService(IDbContextFactory<InnovaParfumDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<ClienteCredito?> GetPerfilCreditoAsync(int idPersona)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.ClientesCredito
            .FirstOrDefaultAsync(c => c.IdPersona == idPersona);
    }

    public async Task<ClienteCredito> AsignarLimiteCreditoAsync(int idPersona, decimal limite, int dias)
    {
        using var context = await _factory.CreateDbContextAsync();
        var perfil = await context.ClientesCredito.FirstOrDefaultAsync(c => c.IdPersona == idPersona);
        
        if (perfil == null)
        {
            perfil = new ClienteCredito
            {
                IdPersona = idPersona,
                LimiteCredito = limite,
                SaldoActual = 0,
                DiasCredito = dias,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            context.ClientesCredito.Add(perfil);
        }
        else
        {
            perfil.LimiteCredito = limite;
            perfil.DiasCredito = dias;
            perfil.Activo = true;
        }

        await context.SaveChangesAsync();
        return perfil;
    }

    public async Task<List<Credito>> GetCreditosActivosAsync(int idPersona)
    {
        using var context = await _factory.CreateDbContextAsync();
        
        // Cargar los créditos activos
        var creditos = await context.Creditos
            .Include(c => c.Persona)
            .Include(c => c.Venta)
            .Include(c => c.Abonos)
            .Where(c => c.IdPersona == idPersona && c.Estado != "ANULADO")
            .ToListAsync();

        bool hasChanges = false;
        var hoy = DateTime.Now.Date;

        // Lazy evaluation para estado VENCIDO
        foreach(var c in creditos)
        {
            if (c.Estado == "ACTIVO" && c.FechaVencimiento.Date < hoy)
            {
                c.Estado = "VENCIDO";
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
        }

        return creditos.OrderBy(c => c.FechaVencimiento).ToList();
    }

    public async Task<List<Credito>> GetAllActiveCreditosAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        
        return await context.Creditos
            .Include(c => c.Persona)
            .Include(c => c.Venta)
            .Include(c => c.Abonos)
            .Where(c => c.Estado == "ACTIVO" || c.Estado == "VENCIDO")
            .OrderBy(c => c.FechaVencimiento)
            .ToListAsync();
    }

    public async Task<List<CreditoAbono>> GetHistorialAbonosAsync(int idCredito)
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.CreditoAbonos
            .Include(a => a.MetodoPago)
            .Include(a => a.Usuario)
            .Where(a => a.IdCredito == idCredito)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<CreditoAbono> RegistrarAbonoAsync(int idCredito, int idUsuario, int idMetodoPago, decimal montoAbonoBase, decimal montoRecibidoMoneda, decimal tasaCambio, decimal vueltoBase, string observacion, string monedaVuelto = "NIO")
    {
        using var context = await _factory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Validar turno abierto
            var turnoAbierto = await context.Turnos.FirstOrDefaultAsync(t => t.IdUsuario == idUsuario && t.IdEstado == 1);
            if (turnoAbierto == null) throw new Exception("No tienes un turno de caja abierto para registrar este abono.");

            var credito = await context.Creditos.FirstOrDefaultAsync(c => c.IdCredito == idCredito);
            if (credito == null) throw new Exception("Crédito no encontrado.");
            if (credito.Estado == "PAGADO") throw new Exception("El crédito ya se encuentra pagado.");
            if (montoAbonoBase <= 0) throw new Exception("El monto a abonar debe ser mayor a 0.");
            if (montoAbonoBase > credito.SaldoPendiente) throw new Exception("El abono no puede superar el saldo pendiente del crédito.");

            var metodoPago = await context.MetodosPagos.FirstOrDefaultAsync(m => m.IdMetodo == idMetodoPago);
            if (metodoPago == null) throw new Exception("Método de pago no válido.");

            var abono = new CreditoAbono
            {
                IdCredito = idCredito,
                Monto = montoAbonoBase,
                MontoRecibidoMoneda = montoRecibidoMoneda,
                TasaCambio = tasaCambio,
                VueltoBase = vueltoBase,
                MonedaVuelto = monedaVuelto,
                IdMetodoPago = idMetodoPago,
                IdUsuario = idUsuario,
                Observacion = observacion,
                Fecha = DateTime.Now
            };

            context.CreditoAbonos.Add(abono);
            
            credito.SaldoPendiente -= montoAbonoBase;
            if (credito.SaldoPendiente == 0)
            {
                credito.Estado = "PAGADO";
            }

            // Actualizar SaldoActual del Cliente
            var perfil = await context.ClientesCredito.FirstOrDefaultAsync(c => c.IdPersona == credito.IdPersona);
            if (perfil != null)
            {
                perfil.SaldoActual -= montoAbonoBase;
                if (perfil.SaldoActual < 0) perfil.SaldoActual = 0;
            }

            // Afectar Caja
            if (metodoPago.AfectaCaja)
            {
                // Solo si el monto recibido es mayor a cero afectamos caja.
                // En abonos registramos como Ingreso el monto en moneda que entregó el cliente, PERO
                // el Vuelto debemos sacarlo en la moneda que el usuario seleccionó.
                
                var ingreso = new MovimientoVario
                {
                    IdTurno = turnoAbierto.IdTurno,
                    Tipo = "INGRESO",
                    IdMoneda = metodoPago.IdMoneda ?? 1,
                    Monto = metodoPago.IdMoneda == 2 ? montoRecibidoMoneda : montoRecibidoMoneda, // Guardamos SIEMPRE el monto físico bruto recibido
                    Concepto = $"Abono a Crédito #{idCredito}",
                    Fecha = DateTime.Now,
                    IdUsuario = idUsuario
                };
                context.MovimientosVarios.Add(ingreso);

                if (vueltoBase > 0)
                {
                    // vueltoBase viene en la moneda del METODO DE PAGO. Si es USD, viene en USD. Si es NIO, viene en NIO.
                    decimal montoFisicoVuelto = vueltoBase; // Asumimos que viene igual a la moneda de pago
                    
                    if (metodoPago.IdMoneda == 2 && monedaVuelto == "NIO")
                    {
                        // Pagó en USD, pero el vuelto es en NIO -> Convertimos a NIO
                        montoFisicoVuelto = vueltoBase * tasaCambio;
                    }
                    else if (metodoPago.IdMoneda == 1 && monedaVuelto == "USD")
                    {
                        // Pagó en NIO, pero el vuelto es en USD -> Convertimos a USD
                        montoFisicoVuelto = vueltoBase / tasaCambio;
                    }

                    var salidaVuelto = new MovimientoVario
                    {
                        IdTurno = turnoAbierto.IdTurno,
                        Tipo = "EGRESO",
                        IdMoneda = monedaVuelto == "USD" ? 2 : 1, 
                        Monto = montoFisicoVuelto,
                        Concepto = $"Vuelto de Abono a Crédito #{idCredito}",
                        Fecha = DateTime.Now,
                        IdUsuario = idUsuario
                    };
                    context.MovimientosVarios.Add(salidaVuelto);
                }

                // Sumar al Turno
                if (metodoPago.IdMoneda == 1) // NIO
                {
                    if (metodoPago.Nombre.Contains("Efectivo", StringComparison.OrdinalIgnoreCase))
                        turnoAbierto.TotalEfectivoBase += montoAbonoBase;
                    else if (metodoPago.Nombre.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase))
                        turnoAbierto.TotalTarjeta += montoAbonoBase;
                    else if (metodoPago.Nombre.Contains("Transferencia", StringComparison.OrdinalIgnoreCase))
                        turnoAbierto.TotalTransferencia += montoAbonoBase;
                }
                else if (metodoPago.IdMoneda == 2) // USD
                {
                    turnoAbierto.TotalEfectivoUsd += montoRecibidoMoneda; // sumamos lo que entro en USD
                    if (vueltoBase > 0)
                    {
                        turnoAbierto.TotalEfectivoBase -= vueltoBase; // Restamos el vuelto de los córdobas
                    }
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return abono;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Error al registrar abono: {ex.Message}");
        }
    }
}

