using InnovaParfumPOS.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

var factory = new InnovaParfumPOS.Backend.Services.InnovaParfumDbContextFactory();
using var context = factory.CreateDbContext(null);
var start = new DateTime(2026, 7, 26);
var end = start.AddDays(1).AddTicks(-1);
var count = context.Ventas.Count(v => v.FechaVenta >= start && v.FechaVenta <= end);
Console.WriteLine($"Ventas on 26/07/2026: {count}");
