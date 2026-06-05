using InnovaParfumPOS.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public class ConfiguracionService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;

    public ConfiguracionService(IDbContextFactory<InnovaParfumDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Obtiene todas las configuraciones como un diccionario (Clave, Valor).
    /// </summary>
    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        using var context = await _factory.CreateDbContextAsync();
        return await context.Configuracions
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Clave, c => c.Valor);
    }

    /// <summary>
    /// Obtiene el valor de una configuración específica.
    /// </summary>
    public async Task<string?> GetSettingAsync(string clave)
    {
        using var context = await _factory.CreateDbContextAsync();
        var setting = await context.Configuracions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Clave == clave);
        return setting?.Valor;
    }

    /// <summary>
    /// Actualiza múltiples configuraciones a la vez o las crea si no existen.
    /// </summary>
    public async Task UpdateSettingsBatchAsync(Dictionary<string, string> settings, int? modificadoPor = null)
    {
        using var context = await _factory.CreateDbContextAsync();
        var claves = settings.Keys.ToList();
        var existingSettings = await context.Configuracions
            .Where(c => claves.Contains(c.Clave))
            .ToListAsync();

        foreach (var kvp in settings)
        {
            var config = existingSettings.FirstOrDefault(c => c.Clave == kvp.Key);
            if (config != null)
            {
                // Actualizar existente
                config.Valor = kvp.Value;
                config.UltimaModificacion = DateTime.Now;
                config.ModificadoPor = modificadoPor;
            }
            else
            {
                // Crear nueva
                context.Configuracions.Add(new Configuracion
                {
                    Clave = kvp.Key,
                    Valor = kvp.Value,
                    UltimaModificacion = DateTime.Now,
                    ModificadoPor = modificadoPor,
                    SoloAdmin = true // Por defecto
                });
            }
        }

        await context.SaveChangesAsync();
    }
}

