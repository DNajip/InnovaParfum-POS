using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public class EmailService
{
    private readonly ConfiguracionService _configService;

    public EmailService(ConfiguracionService configService)
    {
        _configService = configService;
    }

    public async Task<bool> SendDailyReportEmailAsync(byte[] pdfBytes, DateTime date)
    {
        try
        {
            // 1. Obtener parámetros de configuración
            var senderEmail = (await _configService.GetSettingAsync("Correo_Remitente"))?.Trim();
            var senderPassword = (await _configService.GetSettingAsync("Correo_Password"))?.Replace(" ", "").Trim();
            var recipientEmail = (await _configService.GetSettingAsync("Correo_Destinatario"))?.Trim();

            // Validar que las configuraciones estén completas
            if (string.IsNullOrWhiteSpace(senderEmail) || 
                string.IsNullOrWhiteSpace(senderPassword) || 
                string.IsNullOrWhiteSpace(recipientEmail))
            {
                Console.WriteLine("DAILY REPORT EMAIL: No se enviará el correo porque la configuración del remitente o destinatario está incompleta.");
                return false;
            }

            // 2. Crear el mensaje de correo
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("InnovaParfumPOS System", senderEmail));
            message.To.Add(new MailboxAddress("Administración / Jefe", recipientEmail));
            message.Subject = $"Reporte Diario Consolidado — {date:dd/MM/yyyy}";

            // 3. Crear el cuerpo del correo y adjuntar el archivo
            var builder = new BodyBuilder
            {
                TextBody = $"Estimado/a,\n\nSe adjunta el reporte diario consolidado de operaciones correspondiente al día {date:dd/MM/yyyy}.\n\nEste reporte incluye:\n- Detalle de Cajas y Turnos (Teórico, Real y Diferencia)\n- Resumen financiero de ventas netas, brutas y anuladas\n- Desglose por formas de pago\n- Movimientos varios e inventario detallado por cada caja.\n\nAtentamente,\nSistema de Notificaciones Automáticas - InnovaParfumPOS"
            };

            // Adjuntar el PDF
            string filename = $"Reporte_Diario_{date:yyyyMMdd}.pdf";
            builder.Attachments.Add(filename, pdfBytes, new ContentType("application", "pdf"));

            message.Body = builder.ToMessageBody();

            // 4. Conectar y enviar vía SMTP de Gmail
            using (var client = new SmtpClient())
            {
                // Evitar fallos de certificados SSL causados por cortafuegos o antivirus locales que interceptan el tráfico
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                
                try
                {
                    // Intentamos conectar primero por el puerto 465 (SSL/TLS directo), que es el más seguro y menos interceptado por antivirus
                    await client.ConnectAsync("smtp.gmail.com", 465, SecureSocketOptions.SslOnConnect);
                }
                catch (Exception ex465)
                {
                    Console.WriteLine($"DAILY REPORT EMAIL: Intento fallido en puerto 465 ({ex465.Message}). Reintentando con puerto 587 (STARTTLS)...");
                    // Fallback al puerto 587 con STARTTLS si el 465 es bloqueado en la red local
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                }
                
                // Autenticación con contraseña de aplicación
                await client.AuthenticateAsync(senderEmail, senderPassword);
                
                // Enviar el correo
                await client.SendAsync(message);
                
                // Desconectar de forma limpia
                await client.DisconnectAsync(true);
            }

            Console.WriteLine($"DAILY REPORT EMAIL: Reporte diario del {date:dd/MM/yyyy} enviado exitosamente a {recipientEmail}.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DAILY REPORT EMAIL ERROR: Error al enviar correo de reporte diario: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"DAILY REPORT EMAIL ERROR (Inner): {ex.InnerException.Message}");
            }
            return false;
        }
    }
}

