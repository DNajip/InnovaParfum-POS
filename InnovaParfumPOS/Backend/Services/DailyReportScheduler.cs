using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using InnovaParfumPOS.Backend.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public class DailyReportScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DailyReportScheduler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("DAILY REPORT SCHEDULER: Iniciando planificador de reportes automáticos...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var targetTime = new DateTime(now.Year, now.Month, now.Day, 19, 0, 0); // 7:00 PM (19:00)
                bool isDelayed = false;

                // Si ya pasaron las 7:00 PM hoy, revisamos si ya se mandó el de hoy
                if (now >= targetTime)
                {
                    bool sentToday = await CheckIfReportSentTodayAsync();
                    if (!sentToday)
                    {
                        Console.WriteLine("DAILY REPORT SCHEDULER: Se detectó que el reporte de hoy a las 19:00 no fue enviado. Forzando envío retrasado...");
                        isDelayed = true;
                    }
                    else
                    {
                        // Ya se envió hoy, programar para mañana a las 7:00 PM
                        targetTime = targetTime.AddDays(1);
                    }
                }

                if (!isDelayed)
                {
                    var delay = targetTime - now;
                    Console.WriteLine($"DAILY REPORT SCHEDULER: Próxima ejecución programada para {targetTime:dd/MM/yyyy hh:mm tt} (Faltan: {delay.TotalHours:N2} horas).");

                    // Esperar hasta la hora objetivo
                    await Task.Delay(delay, stoppingToken);
                }

                // Ejecutar proceso de generación y envío
                Console.WriteLine("DAILY REPORT SCHEDULER: Es hora de enviar el reporte consolidado diario. Ejecutando...");
                await ExecuteReportAndSendAsync();

                // Ejecutar limpieza de productos (Archivado)
                Console.WriteLine("DAILY REPORT SCHEDULER: Ejecutando limpieza de productos desactivados (Archivado automático)...");
                await ExecuteProductCleanupAsync();

                // Esperar 2 minutos adicionales antes de la siguiente iteración para evitar disparos duplicados rápidos
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("DAILY REPORT SCHEDULER: Planificador cancelado debido al apagado del servicio.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DAILY REPORT SCHEDULER ERROR: Error en el ciclo del planificador: {ex.Message}");
                // Esperar 15 minutos en caso de error crítico antes de reintentar
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }

    private async Task<bool> CheckIfReportSentTodayAsync()
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InnovaParfumDbContext>>();
                using (var context = await contextFactory.CreateDbContextAsync())
                {
                    var config = await context.Configuracions.FirstOrDefaultAsync(c => c.Clave == "LastDailyReportSentDate");
                    if (config != null && config.Valor == DateTime.Today.ToString("yyyy-MM-dd"))
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DAILY REPORT SCHEDULER: Error revisando estado de envío: {ex.Message}");
            return false;
        }
    }

    private async Task ExecuteReportAndSendAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            try
            {
                var dateToReport = DateTime.Today;
                var dateStart = dateToReport.Date;
                var dateEnd = dateToReport.Date.AddDays(1).AddTicks(-1);
                
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InnovaParfumDbContext>>();
                using (var context = await contextFactory.CreateDbContextAsync())
                {
                    // Validar si hubo actividad en caja (turnos abiertos o cerrados hoy)
                    bool hasActiveShifts = await context.Turnos.AnyAsync(t =>
                        t.FechaApertura <= dateEnd && (t.FechaCierre == null || t.FechaCierre >= dateStart));

                    if (!hasActiveShifts)
                    {
                        Console.WriteLine($"DAILY REPORT SCHEDULER: No se detectaron turnos activos o cajas abiertas hoy ({dateToReport:dd/MM/yyyy}). Se omite el envío del reporte.");
                        
                        // Actualizar registro en BD para no intentar enviarlo múltiples veces hoy
                        await MarkReportAsSentAsync(context, dateToReport);
                        return;
                    }

                    var pdfService = scope.ServiceProvider.GetRequiredService<DailyReportPdfService>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                    Console.WriteLine($"DAILY REPORT SCHEDULER: Generando PDF diario para {dateToReport:dd/MM/yyyy}...");
                    byte[] pdfBytes = await pdfService.GenerateDailyReportPdfAsync(dateToReport);

                    Console.WriteLine("DAILY REPORT SCHEDULER: Enviando reporte por correo...");
                    bool sent = await emailService.SendDailyReportEmailAsync(pdfBytes, dateToReport);

                    if (sent)
                    {
                        Console.WriteLine("DAILY REPORT SCHEDULER: Reporte diario procesado y enviado con éxito.");
                        await MarkReportAsSentAsync(context, dateToReport);
                    }
                    else
                    {
                        Console.WriteLine("DAILY REPORT SCHEDULER: No se pudo enviar el reporte por correo. Verifique logs y configuración.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DAILY REPORT SCHEDULER EXECUTION ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"DAILY REPORT SCHEDULER EXECUTION ERROR (Inner): {ex.InnerException.Message}");
                }
            }
        }
    }

    private async Task MarkReportAsSentAsync(InnovaParfumDbContext context, DateTime dateToReport)
    {
        var config = await context.Configuracions.FirstOrDefaultAsync(c => c.Clave == "LastDailyReportSentDate");
        if (config == null)
        {
            config = new Configuracion 
            { 
                Clave = "LastDailyReportSentDate", 
                Valor = dateToReport.ToString("yyyy-MM-dd"), 
                SoloAdmin = true, 
                UltimaModificacion = DateTime.Now 
            };
            context.Configuracions.Add(config);
        }
        else
        {
            config.Valor = dateToReport.ToString("yyyy-MM-dd");
            config.UltimaModificacion = DateTime.Now;
        }
        await context.SaveChangesAsync();
    }

    private async Task ExecuteProductCleanupAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            try
            {
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<InnovaParfumDbContext>>();
                using (var context = await contextFactory.CreateDbContextAsync())
                {
                    var thresholdDate = DateTime.Now.AddMonths(-6);
                    
                    var productosAArchivar = await context.Productos
                        .Where(p => !p.Activo && !p.Archivado && p.FechaDesactivacion != null && p.FechaDesactivacion <= thresholdDate)
                        .ToListAsync();

                    if (productosAArchivar.Any())
                    {
                        Console.WriteLine($"DAILY REPORT SCHEDULER: Se encontraron {productosAArchivar.Count} productos inactivos por más de 6 meses. Procediendo a archivar...");
                        foreach (var prod in productosAArchivar)
                        {
                            prod.Archivado = true;
                        }
                        
                        await context.SaveChangesAsync();
                        Console.WriteLine($"DAILY REPORT SCHEDULER: {productosAArchivar.Count} productos han sido archivados exitosamente.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DAILY REPORT SCHEDULER CLEANUP ERROR: {ex.Message}");
            }
        }
    }
}

