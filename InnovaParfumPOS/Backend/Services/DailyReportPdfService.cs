using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using InnovaParfumPOS.Backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public class DailyReportPdfService
{
    private readonly IDbContextFactory<InnovaParfumDbContext> _factory;
    private readonly IWebHostEnvironment _env;

    public DailyReportPdfService(IDbContextFactory<InnovaParfumDbContext> factory, IWebHostEnvironment env)
    {
        _factory = factory;
        _env = env;
    }

    public async Task<byte[]> GenerateDailyReportPdfAsync(DateTime date)
    {
        using var context = await _factory.CreateDbContextAsync();

        // 1. Obtener configuraciones de la empresa
        var settings = await context.Configuracions.AsNoTracking().ToDictionaryAsync(c => c.Clave, c => c.Valor);
        var nombreEmpresa = settings.GetValueOrDefault("Empresa_Nombre", "INNOVATEC POS");
        var ruc = settings.GetValueOrDefault("Empresa_RUC", "");
        var telefono = settings.GetValueOrDefault("Empresa_Telefono", "");
        var direccion = settings.GetValueOrDefault("Empresa_Direccion", "");
        var logoSetting = settings.GetValueOrDefault("Empresa_Logo", "images/logo.png");

        // 2. Obtener Turnos del día
        var dateStart = date.Date;
        var dateEnd = date.Date.AddDays(1).AddTicks(-1);

        var turnos = await context.Turnos
            .Include(t => t.IdUsuarioNavigation)
            .Include(t => t.MovimientosVarios)
            .Include(t => t.Venta)
                .ThenInclude(v => v.Pagos)
                    .ThenInclude(p => p.IdMetodoPagoNavigation)
            .Where(t => t.FechaApertura <= dateEnd && (t.FechaCierre == null || t.FechaCierre >= dateStart))
            .OrderBy(t => t.FechaApertura)
            .ToListAsync();

        // 3. Ventas Totales y Métodos de Pago
        var ventasDia = await context.Ventas
            .Include(v => v.Pagos)
                .ThenInclude(p => p.IdMetodoPagoNavigation)
            .Where(v => v.FechaVenta >= dateStart && v.FechaVenta <= dateEnd)
            .ToListAsync();

        var ventasActivas = ventasDia.Where(v => !v.Anulada).ToList();
        var ventasAnuladas = ventasDia.Where(v => v.Anulada).ToList();

        decimal ventasBrutas = ventasActivas.Sum(v => v.TotalNio + v.DescuentoNio); // Venta antes de descuento
        decimal descuentos = ventasActivas.Sum(v => v.DescuentoNio);
        decimal ventasNetas = ventasActivas.Sum(v => v.TotalNio); // Neto con descuento aplicado

        var desglosePagos = ventasActivas
            .SelectMany(v => v.Pagos)
            .GroupBy(p => p.IdMetodoPagoNavigation.Nombre)
            .Select(g => new
            {
                Metodo = g.Key,
                Total = g.Sum(p => p.MontoEnNio - (p.VueltoNio ?? 0))
            })
            .ToList();

        using (var stream = new MemoryStream())
        {
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

            // Paleta de Colores
            Color primaryColor = new DeviceRgb(15, 23, 42); // Slate 900 (Azul oscuro / grisáceo premium)
            Color secondaryColor = new DeviceRgb(37, 99, 235); // Blue 600
            Color lightGray = new DeviceRgb(248, 250, 252); // Slate 50
            Color textDark = new DeviceRgb(51, 65, 85); // Slate 700

            // --- CABECERA ---
            Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 85 })).UseAllAvailableWidth();
            headerTable.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            // Renderizado del Logo
            Image? img = null;
            if (!string.IsNullOrEmpty(logoSetting))
            {
                try
                {
                    if (logoSetting.StartsWith("data:image"))
                    {
                        var commaIndex = logoSetting.IndexOf(',');
                        if (commaIndex >= 0)
                        {
                            var bytes = Convert.FromBase64String(logoSetting.Substring(commaIndex + 1));
                            ImageData data = ImageDataFactory.Create(bytes);
                            img = new Image(data).SetWidth(50).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                        }
                    }
                    else
                    {
                        string logoPath = System.IO.Path.Combine(_env.WebRootPath, logoSetting);
                        if (File.Exists(logoPath))
                        {
                            ImageData data = ImageDataFactory.Create(logoPath);
                            img = new Image(data).SetWidth(50).SetHeight(50).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                        }
                    }
                }
                catch
                {
                    // Ignorar error de logo para no romper el PDF
                }
            }

            if (img != null)
            {
                headerTable.AddCell(new Cell().Add(img).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }
            else
            {
                headerTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            }

            Cell bizInfoCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingLeft(10);
            bizInfoCell.Add(new Paragraph(nombreEmpresa.ToUpper()).SetFontSize(16).SetFont(boldFont).SetFontColor(primaryColor));
            
            string subHeader = "";
            if (!string.IsNullOrEmpty(ruc)) subHeader += $"RUC: {ruc}";
            if (!string.IsNullOrEmpty(telefono)) subHeader += (subHeader == "" ? "" : "  |  ") + $"Tel: {telefono}";
            if (!string.IsNullOrEmpty(direccion)) subHeader += (subHeader == "" ? "" : "  |  ") + direccion;

            if (!string.IsNullOrEmpty(subHeader))
            {
                bizInfoCell.Add(new Paragraph(subHeader).SetFontSize(8.5f).SetFont(regularFont).SetFontColor(textDark));
            }
            bizInfoCell.Add(new Paragraph($"REPORTE CONSOLIDADO DIARIO DE OPERACIONES").SetFontSize(11).SetFont(boldFont).SetFontColor(secondaryColor).SetMarginTop(4));
            bizInfoCell.Add(new Paragraph($"Fecha de Reporte: {date:dd/MM/yyyy}  |  Generado: {DateTime.Now:dd/MM/yyyy hh:mm tt}").SetFontSize(8).SetFont(italicFont).SetFontColor(ColorConstants.GRAY));

            headerTable.AddCell(bizInfoCell);
            document.Add(headerTable);
            document.Add(new Paragraph("\n").SetFontSize(4));

            // --- SECCIÓN 1: CAJAS Y TURNOS DEL DÍA ---
            document.Add(new Paragraph("1. Auditoría de Cajas y Turnos").SetFontSize(12).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(6));
            
            Table turnosTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 18, 14, 14, 11, 11, 11, 11 })).UseAllAvailableWidth();
            turnosTable.AddHeaderCell(CreateHeaderCell("Turno", primaryColor, boldFont));
            turnosTable.AddHeaderCell(CreateHeaderCell("Cajero", primaryColor, boldFont));
            turnosTable.AddHeaderCell(CreateHeaderCell("Apertura", primaryColor, boldFont));
            turnosTable.AddHeaderCell(CreateHeaderCell("Cierre", primaryColor, boldFont));
            turnosTable.AddHeaderCell(CreateHeaderCell("Inicial", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
            turnosTable.AddHeaderCell(CreateHeaderCell("Efectivo", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
            turnosTable.AddHeaderCell(CreateHeaderCell("Contado", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
            turnosTable.AddHeaderCell(CreateHeaderCell("Diferencia", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));

            if (!turnos.Any())
            {
                turnosTable.AddCell(new Cell(1, 8).Add(new Paragraph("No se registraron turnos abiertos o cerrados en este día.").SetFontSize(9).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER)).SetPadding(10));
            }
            else
            {
                int rowIdx = 0;
                foreach (var t in turnos)
                {
                    var bg = rowIdx % 2 == 0 ? ColorConstants.WHITE : lightGray;
                    decimal ingresosVarios = t.MovimientosVarios.Where(m => m.Tipo == "INGRESO").Sum(m => m.Monto);
                    decimal egresosVarios = t.MovimientosVarios.Where(m => m.Tipo == "EGRESO").Sum(m => m.Monto);
                    decimal saldoTeorico = t.MontoInicialNio + t.TotalEfectivoNio + ingresosVarios - egresosVarios;
                    decimal saldoReal = t.MontoContadoNio ?? 0;
                    decimal diferencia = saldoReal - saldoTeorico;

                    turnosTable.AddCell(new Cell().Add(new Paragraph($"#{t.IdTurno}").SetFontSize(8.5f).SetFont(boldFont)).SetBackgroundColor(bg).SetPadding(4));
                    turnosTable.AddCell(new Cell().Add(new Paragraph(t.IdUsuarioNavigation?.Username ?? "N/A").SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4));
                    turnosTable.AddCell(new Cell().Add(new Paragraph(t.FechaApertura.ToString("dd/MM HH:mm")).SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(4));
                    turnosTable.AddCell(new Cell().Add(new Paragraph(t.FechaCierre?.ToString("dd/MM HH:mm") ?? "EN CURSO").SetFontSize(8f).SetFont(t.FechaCierre == null ? boldFont : regularFont).SetFontColor(t.FechaCierre == null ? secondaryColor : textDark)).SetBackgroundColor(bg).SetPadding(4));
                    turnosTable.AddCell(new Cell().Add(new Paragraph($"C$ {t.MontoInicialNio:N2}").SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    turnosTable.AddCell(new Cell().Add(new Paragraph($"C$ {t.TotalEfectivoNio:N2}").SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    turnosTable.AddCell(new Cell().Add(new Paragraph(t.FechaCierre != null ? $"C$ {saldoReal:N2}" : "--").SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));

                    var diffColor = diferencia < 0 ? new DeviceRgb(220, 38, 38) : (diferencia > 0 ? new DeviceRgb(217, 119, 6) : new DeviceRgb(22, 163, 74));
                    var diffText = t.FechaCierre != null ? $"C$ {diferencia:N2}" : "--";
                    turnosTable.AddCell(new Cell().Add(new Paragraph(diffText).SetFontSize(8.5f).SetFont(boldFont).SetFontColor(diffColor)).SetBackgroundColor(bg).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    
                    rowIdx++;
                }
            }
            document.Add(turnosTable);
            document.Add(new Paragraph("\n").SetFontSize(6));

            // --- SECCIÓN 2: VENTAS TOTALES Y MÉTODOS DE PAGO ---
            document.Add(new Paragraph("2. Resumen Financiero").SetFontSize(12).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(6));

            Table financeContainer = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            financeContainer.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

            // Celda Izquierda: Resumen de Ventas
            Cell leftCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingRight(10);
            leftCell.Add(new Paragraph("Ventas Totales").SetFontSize(10).SetFont(boldFont).SetFontColor(secondaryColor).SetMarginBottom(4));
            
            Table salesSummaryTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 })).UseAllAvailableWidth();
            salesSummaryTable.AddCell(CreateLabelCell("Ventas Brutas (Sin desc.):", regularFont));
            salesSummaryTable.AddCell(CreateValueCell($"C$ {ventasBrutas:N2}", boldFont));
            salesSummaryTable.AddCell(CreateLabelCell("Descuentos Aplicados:", regularFont));
            salesSummaryTable.AddCell(CreateValueCell($"- C$ {descuentos:N2}", regularFont).SetFontColor(new DeviceRgb(220, 38, 38)));
            salesSummaryTable.AddCell(CreateLabelCell("Ventas Netas (Con desc.):", boldFont));
            salesSummaryTable.AddCell(CreateValueCell($"C$ {ventasNetas:N2}", boldFont).SetFontColor(new DeviceRgb(22, 163, 74)));
            salesSummaryTable.AddCell(CreateLabelCell("Total Transacciones:", regularFont));
            salesSummaryTable.AddCell(CreateValueCell(ventasActivas.Count.ToString(), regularFont));
            salesSummaryTable.AddCell(CreateLabelCell("Facturas Anuladas:", regularFont));
            salesSummaryTable.AddCell(CreateValueCell(ventasAnuladas.Count.ToString(), regularFont).SetFontColor(new DeviceRgb(220, 38, 38)));

            leftCell.Add(salesSummaryTable);
            financeContainer.AddCell(leftCell);

            // Celda Derecha: Desglose por Formas de Pago
            Cell rightCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingLeft(10);
            rightCell.Add(new Paragraph("Ingresos por Formas de Pago").SetFontSize(10).SetFont(boldFont).SetFontColor(secondaryColor).SetMarginBottom(4));

            Table paymentsTable = new Table(UnitValue.CreatePercentArray(new float[] { 60, 40 })).UseAllAvailableWidth();
            paymentsTable.AddHeaderCell(CreateHeaderCell("Método", secondaryColor, boldFont));
            paymentsTable.AddHeaderCell(CreateHeaderCell("Total Ingresado", secondaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));

            if (!desglosePagos.Any())
            {
                paymentsTable.AddCell(new Cell(1, 2).Add(new Paragraph("No se registran pagos hoy.").SetFontSize(9).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER)).SetPadding(8));
            }
            else
            {
                int r = 0;
                foreach (var p in desglosePagos)
                {
                    var bg = r % 2 == 0 ? ColorConstants.WHITE : lightGray;
                    paymentsTable.AddCell(new Cell().Add(new Paragraph(p.Metodo).SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4));
                    paymentsTable.AddCell(new Cell().Add(new Paragraph($"C$ {p.Total:N2}").SetFontSize(8.5f).SetFont(boldFont)).SetBackgroundColor(bg).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
                    r++;
                }
                // Fila Total
                paymentsTable.AddCell(new Cell().Add(new Paragraph("Total Conciliado").SetFontSize(8.5f).SetFont(boldFont)).SetBackgroundColor(lightGray).SetPadding(4));
                paymentsTable.AddCell(new Cell().Add(new Paragraph($"C$ {ventasNetas:N2}").SetFontSize(8.5f).SetFont(boldFont).SetFontColor(secondaryColor)).SetBackgroundColor(lightGray).SetPadding(4).SetTextAlignment(TextAlignment.RIGHT));
            }

            rightCell.Add(paymentsTable);
            financeContainer.AddCell(rightCell);

            document.Add(financeContainer);
            document.Add(new Paragraph("\n").SetFontSize(8));

            // --- SECCIÓN 3: MOVIMIENTOS DETALLADOS POR CAJA ---
            document.Add(new Paragraph("3. Desglose Operativo y de Inventario por Caja").SetFontSize(12).SetFont(boldFont).SetFontColor(primaryColor).SetMarginBottom(8));

            if (!turnos.Any())
            {
                document.Add(new Paragraph("No hay operaciones detalladas porque no se abrieron cajas hoy.").SetFontSize(9).SetFont(italicFont).SetFontColor(ColorConstants.GRAY));
            }
            else
            {
                foreach (var t in turnos)
                {
                    // Subsección por turno
                    var titleParagraph = new Paragraph($"Caja/Turno #{t.IdTurno}  —  Cajero: {t.IdUsuarioNavigation?.Username ?? "N/A"}  (Apertura: {t.FechaApertura:hh:mm tt})")
                        .SetFontSize(9.5f)
                        .SetFont(boldFont)
                        .SetFontColor(primaryColor)
                        .SetBackgroundColor(lightGray)
                        .SetPadding(5)
                        .SetMarginTop(10)
                        .SetMarginBottom(4);
                    document.Add(titleParagraph);

                    // 3.1 Movimientos Varios (Ingreso / Egreso manual)
                    var movimientosVarios = t.MovimientosVarios.OrderBy(m => m.Fecha).ToList();
                    document.Add(new Paragraph("• Movimientos Varios de Efectivo (Entradas y Salidas Manuales)").SetFontSize(8.5f).SetFont(boldFont).SetFontColor(secondaryColor).SetMarginBottom(3));
                    
                    Table movsVariosTable = new Table(UnitValue.CreatePercentArray(new float[] { 15, 55, 15, 15 })).UseAllAvailableWidth();
                    movsVariosTable.AddHeaderCell(CreateHeaderCell("Tipo", primaryColor, boldFont));
                    movsVariosTable.AddHeaderCell(CreateHeaderCell("Concepto / Descripción", primaryColor, boldFont));
                    movsVariosTable.AddHeaderCell(CreateHeaderCell("Hora", primaryColor, boldFont).SetTextAlignment(TextAlignment.CENTER));
                    movsVariosTable.AddHeaderCell(CreateHeaderCell("Monto", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));

                    if (!movimientosVarios.Any())
                    {
                        movsVariosTable.AddCell(new Cell(1, 4).Add(new Paragraph("No se registraron entradas o salidas manuales de efectivo en esta caja.").SetFontSize(8f).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER)).SetPadding(6));
                    }
                    else
                    {
                        int row = 0;
                        foreach (var m in movimientosVarios)
                        {
                            var bg = row % 2 == 0 ? ColorConstants.WHITE : lightGray;
                            var tipoColor = m.Tipo == "INGRESO" ? new DeviceRgb(22, 163, 74) : new DeviceRgb(220, 38, 38);
                            
                            movsVariosTable.AddCell(new Cell().Add(new Paragraph(m.Tipo).SetFontSize(8f).SetFont(boldFont).SetFontColor(tipoColor)).SetBackgroundColor(bg).SetPadding(3));
                            movsVariosTable.AddCell(new Cell().Add(new Paragraph(m.Concepto).SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3));
                            movsVariosTable.AddCell(new Cell().Add(new Paragraph(m.Fecha.ToString("hh:mm tt")).SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.CENTER));
                            
                            var pref = m.Tipo == "EGRESO" ? "-" : "";
                            movsVariosTable.AddCell(new Cell().Add(new Paragraph($"{pref}C$ {m.Monto:N2}").SetFontSize(8f).SetFont(boldFont).SetFontColor(tipoColor)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));
                            row++;
                        }
                    }
                    document.Add(movsVariosTable);
                    document.Add(new Paragraph("\n").SetFontSize(4));

                    // 3.2 Salidas de Inventario Detallado por Ventas de la Caja
                    var ventasCaja = t.Venta.Where(v => !v.Anulada).ToList();
                    document.Add(new Paragraph("• Detalle de Ventas y Salidas de Inventario").SetFontSize(8.5f).SetFont(boldFont).SetFontColor(secondaryColor).SetMarginBottom(3));

                    // Tabla columnas: Factura, Producto, Cantidad, Método Pago, Pago Con, Vuelto, Descuento, Subtotal
                    Table itemsTable = new Table(UnitValue.CreatePercentArray(new float[] { 12, 28, 8, 13, 10, 9, 10, 10 })).UseAllAvailableWidth();
                    itemsTable.AddHeaderCell(CreateHeaderCell("Factura", primaryColor, boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Producto / Artículo", primaryColor, boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Cant.", primaryColor, boldFont).SetTextAlignment(TextAlignment.CENTER));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Método", primaryColor, boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Pago Con", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Vuelto", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Descuento", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Subtotal", primaryColor, boldFont).SetTextAlignment(TextAlignment.RIGHT));

                    if (!ventasCaja.Any())
                    {
                        itemsTable.AddCell(new Cell(1, 8).Add(new Paragraph("No se registraron ventas en esta caja.").SetFontSize(8f).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER)).SetPadding(6));
                    }
                    else
                    {
                        int row = 0;
                        foreach (var v in ventasCaja)
                        {
                            var listDetalles = await context.VentaDetalles.Include(d => d.IdProductoNavigation).Where(d => d.IdVenta == v.IdVenta).ToListAsync();
                            var pagoPrincipal = v.Pagos.OrderByDescending(p => p.MontoEnNio).FirstOrDefault();
                            
                            string metodoStr = pagoPrincipal?.IdMetodoPagoNavigation?.Nombre ?? "N/A";
                            if (v.Pagos.Count > 1)
                            {
                                metodoStr = string.Join("+", v.Pagos.Select(p => p.IdMetodoPagoNavigation.Nombre).Distinct());
                            }

                            decimal totalPagoCon = v.Pagos.Sum(p => p.MontoRecibido == null || p.MontoRecibido == 0 ? p.MontoEnNio : p.MontoRecibido.Value);
                            decimal totalVuelto = v.Pagos.Sum(p => p.VueltoNio ?? 0);

                            // Si la venta tiene múltiples detalles, agrupamos la información para que sea legible
                            bool firstLineOfSale = true;
                            foreach (var det in listDetalles)
                            {
                                var bg = row % 2 == 0 ? ColorConstants.WHITE : lightGray;

                                // Factura
                                if (firstLineOfSale)
                                {
                                    itemsTable.AddCell(new Cell(listDetalles.Count, 1).Add(new Paragraph(v.NumeroFactura ?? $"FAC-{v.IdVenta}").SetFontSize(8f).SetFont(boldFont)).SetBackgroundColor(bg).SetPadding(3).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                }

                                // Producto
                                string prodDesc = det.DescripcionSnap;
                                itemsTable.AddCell(new Cell().Add(new Paragraph(prodDesc).SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3));

                                // Cantidad
                                itemsTable.AddCell(new Cell().Add(new Paragraph(det.Cantidad.ToString()).SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.CENTER));

                                // Método Pago, Pago con, Vuelto, Descuento (Una sola celda por Venta)
                                if (firstLineOfSale)
                                {
                                    itemsTable.AddCell(new Cell(listDetalles.Count, 1).Add(new Paragraph(metodoStr).SetFontSize(7.5f)).SetBackgroundColor(bg).SetPadding(3).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                    itemsTable.AddCell(new Cell(listDetalles.Count, 1).Add(new Paragraph($"C$ {totalPagoCon:N2}").SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                    itemsTable.AddCell(new Cell(listDetalles.Count, 1).Add(new Paragraph(totalVuelto > 0 ? $"C$ {totalVuelto:N2}" : "--").SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE));

                                    var descText = v.DescuentoNio > 0 ? $"C$ {v.DescuentoNio:N2}" : "--";
                                    var descColor = v.DescuentoNio > 0 ? new DeviceRgb(220, 38, 38) : textDark;
                                    var descFont = v.DescuentoNio > 0 ? boldFont : regularFont;
                                    itemsTable.AddCell(new Cell(listDetalles.Count, 1).Add(new Paragraph(descText).SetFontSize(8f).SetFont(descFont).SetFontColor(descColor)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                                }

                                // Subtotal del artículo
                                itemsTable.AddCell(new Cell().Add(new Paragraph($"C$ {det.SubtotalNio:N2}").SetFontSize(8f)).SetBackgroundColor(bg).SetPadding(3).SetTextAlignment(TextAlignment.RIGHT));

                                firstLineOfSale = false;
                                row++;
                            }
                        }
                    }
                    document.Add(itemsTable);
                    document.Add(new Paragraph("\n").SetFontSize(4));
                }
            }

            // --- PIE DE PÁGINA ---
            document.Add(new Paragraph("\n"));
            Paragraph footerNote = new Paragraph($"Reporte Diario Automatizado de Operaciones — Diseñado para {nombreEmpresa}\nFin de Reporte Consolidado Diario de Cajas e Inventario.");
            footerNote.SetFontSize(7.5f).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER).SetFontColor(ColorConstants.GRAY);
            document.Add(footerNote);

            document.Close();
            return stream.ToArray();
        }
    }

    private Cell CreateHeaderCell(string text, Color bg, PdfFont font)
    {
        return new Cell().Add(new Paragraph(text).SetFont(font).SetFontColor(ColorConstants.WHITE).SetFontSize(8.5f)).SetBackgroundColor(bg).SetPadding(4);
    }

    private Cell CreateLabelCell(string text, PdfFont font)
    {
        return new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(9).SetFontColor(new DeviceRgb(51, 65, 85))).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3);
    }

    private Cell CreateValueCell(string text, PdfFont font)
    {
        return new Cell().Add(new Paragraph(text).SetFont(font).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(3);
    }
}

