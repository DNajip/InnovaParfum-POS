using InnovaParfumPOS.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

var factory = new InnovaParfumPOS.Backend.Services.InnovaParfumDbContextFactory();
using var context = factory.CreateDbContext(null);
var ventas = context.Ventas.Select(v => new { v.IdVenta, v.FechaVenta, v.TotalBase, v.IdTurno }).ToList();
foreach(var v in ventas) {
    Console.WriteLine($"Venta {v.IdVenta} - Fecha: {v.FechaVenta} - Total: {v.TotalBase} - Turno: {v.IdTurno}");
}
