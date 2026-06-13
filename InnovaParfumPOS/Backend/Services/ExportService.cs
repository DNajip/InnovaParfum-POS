using InnovaParfumPOS.Backend.Models;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Image;
using iText.Kernel.Geom;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Services;

public interface IExportService
{
    byte[] GenerateInventoryExcel(List<Producto> products);
    Task<byte[]> GenerateInventoryPdfAsync(List<Producto> products);
}

public class ExportService : IExportService
{
    private readonly IWebHostEnvironment _env;
    private readonly ConfiguracionService _configService;

    public ExportService(IWebHostEnvironment env, ConfiguracionService configService)
    {
        _env = env;
        _configService = configService;
    }

    public byte[] GenerateInventoryExcel(List<Producto> products)
    {
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Auditoría Inventario");
            
            // TITULOS DE CABECERA
            string[] headers = { 
                "CÓDIGO", "PRODUCTO", "MARCA", "GÉNERO", "CONCENTRACIÓN", "ORIGEN/TIPO", 
                "CATEGORÍA", "PRECIO VENTA", "STOCK ACT.", "STOCK MIN.", 
                "ESTADO SALUD", "ÚLTIMA MOV.", "VALORIZACIÓN" 
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b"); // Slate 800
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 2;
            foreach (var p in products)
            {
                worksheet.Cell(row, 1).Value = p.CodigoBarras ?? "S/N";
                worksheet.Cell(row, 2).Value = p.Nombre;
                worksheet.Cell(row, 3).Value = p.Marca ?? "N/A";
                worksheet.Cell(row, 4).Value = p.IdGeneroNavigation.DescGenero ?? "N/A";
                worksheet.Cell(row, 5).Value = p.IdConcentracionNavigation.Nombre ?? "N/A";
                worksheet.Cell(row, 6).Value = p.IdOrigenNavigation.Nombre ?? "N/A";
                worksheet.Cell(row, 7).Value = p.IdCategoriaNavigation?.Nombre ?? "S/C";
                
                // Financiero
                worksheet.Cell(row, 8).Value = p.PrecioMinorista ?? 0;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "$ #,##0.00";

                // Stock
                worksheet.Cell(row, 9).Value = p.StockActual;
                worksheet.Cell(row, 10).Value = p.StockMinimo;

                // Estado Salud (Lógica)
                var statusCell = worksheet.Cell(row, 11);
                if (p.StockActual == 0) {
                    statusCell.Value = "AGOTADO";
                    statusCell.Style.Font.FontColor = XLColor.White;
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626"); // Red
                } else if (p.StockActual <= p.StockMinimo) {
                    statusCell.Value = "CRÍTICO";
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f59e0b"); // Amber
                } else {
                    statusCell.Value = "ESTABLE";
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#10b981"); // Emerald
                    statusCell.Style.Font.FontColor = XLColor.White;
                }
                statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Última Mov
                var lastMov = p.Movimientos.OrderByDescending(m => m.FechaMov).FirstOrDefault();
                worksheet.Cell(row, 12).Value = lastMov?.FechaMov.ToString("dd/MM/yyyy") ?? "S/R";
                worksheet.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Valorización (FÓRMULA VIVA)
                // Columna H(8) * I(9)
                worksheet.Cell(row, 13).FormulaA1 = $"H{row}*I{row}";
                worksheet.Cell(row, 13).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Cell(row, 13).Style.Font.Bold = true;

                row++;
            }

            // --- ESTILOS FINALES ---
            var range = worksheet.Range(1, 1, row - 1, headers.Length);
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            worksheet.Columns().AdjustToContents();
            worksheet.SheetView.FreezeRows(1); // Congelar Cabecera

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }

    public async Task<byte[]> GenerateInventoryPdfAsync(List<Producto> products)
    {
        try
        {
            Console.WriteLine("PDF: Starting detailed horizontal generation...");
            
            // Cargar configuraciones de la empresa
            var settings = await _configService.GetAllSettingsAsync();
            var nombre = settings.GetValueOrDefault("Empresa_Nombre", "INNOVATEC");
            var ruc = settings.GetValueOrDefault("Empresa_RUC", "");
            var telefono = settings.GetValueOrDefault("Empresa_Telefono", "");
            var direccion = settings.GetValueOrDefault("Empresa_Direccion", "");
            var logoSetting = settings.GetValueOrDefault("Empresa_Logo", "images/logo.png");

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new PdfDocument(writer);
                // MODO HORIZONTAL (LANDSCAPE)
                var document = new Document(pdf, PageSize.A4.Rotate());
                document.SetMargins(25, 25, 25, 25);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                // --- HEADER SECTION ---
                Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 90 })).UseAllAvailableWidth();
                headerTable.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

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
                                var cleanBase64 = logoSetting.Substring(commaIndex + 1);
                                var bytes = Convert.FromBase64String(cleanBase64);
                                ImageData data = ImageDataFactory.Create(bytes);
                                img = new Image(data).SetWidth(50).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                            }
                        }
                        else
                        {
                            string logoPath = System.IO.Path.Combine(_env.WebRootPath, logoSetting);
                            if (System.IO.File.Exists(logoPath))
                            {
                                ImageData data = ImageDataFactory.Create(logoPath);
                                img = new Image(data).SetWidth(50).SetHorizontalAlignment(HorizontalAlignment.LEFT);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al renderizar imagen en PDF: {ex.Message}");
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

                Cell bizInfoCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingLeft(5);
                bizInfoCell.Add(new Paragraph(nombre.ToUpper()).SetFontSize(20).SetFont(boldFont).SetFontColor(iText.Kernel.Colors.ColorConstants.DARK_GRAY).SetFixedLeading(18));
                
                string subHeader = "";
                if (!string.IsNullOrEmpty(ruc)) subHeader += $"RUC: {ruc}";
                if (!string.IsNullOrEmpty(telefono)) subHeader += (subHeader == "" ? "" : " | ") + $"Contacto: {telefono}";
                
                if (!string.IsNullOrEmpty(subHeader))
                {
                    bizInfoCell.Add(new Paragraph(subHeader).SetFontSize(9).SetFont(italicFont).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));
                }
                
                if (!string.IsNullOrEmpty(direccion))
                {
                    bizInfoCell.Add(new Paragraph(direccion).SetFontSize(9).SetFont(regularFont));
                }
                headerTable.AddCell(bizInfoCell);

                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // --- SUMMARY KPI SECTION (Minimalist) ---
                int totalStock = products.Sum(p => p.StockActual);
                int stockBajo = products.Count(p => p.StockActual > 0 && p.StockActual <= p.StockMinimo);
                int sinStock = products.Count(p => p.StockActual == 0);

                Table kpiTable = new Table(UnitValue.CreatePercentArray(new float[] { 33.3f, 33.3f, 33.3f })).UseAllAvailableWidth();
                kpiTable.SetMarginBottom(15);

                kpiTable.AddCell(CreateKpiCell("TOTAL UNIDADES", totalStock.ToString(), iText.Kernel.Colors.ColorConstants.DARK_GRAY, boldFont));
                kpiTable.AddCell(CreateKpiCell("PRODUCTOS EN CRÍTICO", stockBajo.ToString(), new iText.Kernel.Colors.DeviceRgb(217, 119, 6), boldFont));
                kpiTable.AddCell(CreateKpiCell("PRODUCTOS AGOTADOS", sinStock.ToString(), new iText.Kernel.Colors.DeviceRgb(220, 38, 38), boldFont));

                document.Add(kpiTable);

                // --- DATA TABLE ---
                // 7 Columnas: Código, Producto Detallado, Categoría, Precio Venta, Stock (A/M), Estado, Última Mov.
                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 14, 28, 14, 10, 10, 10, 14 }));
                table.UseAllAvailableWidth();
                
                iText.Kernel.Colors.Color headerBg = new iText.Kernel.Colors.DeviceRgb(30, 41, 59); // Slate 800
                
                table.AddHeaderCell(CreateStyledHeaderCell("Código", headerBg, boldFont));
                table.AddHeaderCell(CreateStyledHeaderCell("Producto Detalle", headerBg, boldFont));
                table.AddHeaderCell(CreateStyledHeaderCell("Categoría", headerBg, boldFont));
                table.AddHeaderCell(CreateStyledHeaderCell("Precio Venta", headerBg, boldFont).SetTextAlignment(TextAlignment.RIGHT));
                table.AddHeaderCell(CreateStyledHeaderCell("Stock (A/M)", headerBg, boldFont).SetTextAlignment(TextAlignment.CENTER));
                table.AddHeaderCell(CreateStyledHeaderCell("Estado", headerBg, boldFont).SetTextAlignment(TextAlignment.CENTER));
                table.AddHeaderCell(CreateStyledHeaderCell("Última Mov.", headerBg, boldFont).SetTextAlignment(TextAlignment.CENTER));

                int count = 0;
                foreach (var p in products)
                {
                    bool isEven = count % 2 == 0;
                    iText.Kernel.Colors.Color rowBg = isEven ? iText.Kernel.Colors.ColorConstants.WHITE : new iText.Kernel.Colors.DeviceRgb(248, 250, 252); 
                    
                    // Código
                    table.AddCell(new Cell().Add(new Paragraph(p.CodigoBarras ?? "S/N")).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f));
                    
                    // Producto Detallado
                    string fullDesc = $"{p.Nombre} {p.Marca} {p.IdGeneroNavigation.DescGenero}";
                    if (!string.IsNullOrEmpty(p.IdConcentracionNavigation.Nombre) || !string.IsNullOrEmpty(p.IdOrigenNavigation.Nombre))
                    {
                        fullDesc += $" ({p.IdConcentracionNavigation.Nombre}{(string.IsNullOrEmpty(p.IdOrigenNavigation.Nombre) ? "" : " - " + p.IdOrigenNavigation.Nombre)})";
                    }
                    table.AddCell(new Cell().Add(new Paragraph(fullDesc)).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f));
                    
                    // Categoría
                    table.AddCell(new Cell().Add(new Paragraph(p.IdCategoriaNavigation?.Nombre ?? "S/C")).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f));
                    
                    // Precio Venta
                    table.AddCell(new Cell().Add(new Paragraph($"C$ {p.PrecioMinorista:N2}")).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f).SetTextAlignment(TextAlignment.RIGHT));
                    
                    // Stock (A/M)
                    table.AddCell(new Cell().Add(new Paragraph($"{p.StockActual} / {p.StockMinimo}")).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f).SetTextAlignment(TextAlignment.CENTER));
                    
                    // Estado
                    Paragraph estadoPara = new Paragraph();
                    iText.Kernel.Colors.Color statusColor = iText.Kernel.Colors.ColorConstants.DARK_GRAY;
                    string statusText = "OK";

                    if (p.StockActual == 0)
                    {
                        statusText = "AGOTADO";
                        statusColor = new iText.Kernel.Colors.DeviceRgb(220, 38, 38);
                    }
                    else if (p.StockActual <= p.StockMinimo)
                    {
                        statusText = "CRÍTICO";
                        statusColor = new iText.Kernel.Colors.DeviceRgb(217, 119, 6);
                    }
                    else
                    {
                        statusText = "ESTABLE";
                        statusColor = new iText.Kernel.Colors.DeviceRgb(22, 163, 74);
                    }
                    estadoPara.Add(statusText);
                    estadoPara.SetFont(boldFont).SetFontColor(statusColor);
                    table.AddCell(new Cell().Add(estadoPara).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f).SetTextAlignment(TextAlignment.CENTER));
                    
                    // Última Movización
                    var lastMov = p.Movimientos.OrderByDescending(m => m.FechaMov).FirstOrDefault();
                    string movDate = lastMov?.FechaMov.ToString("dd/MM/yy") ?? "S/R";
                    table.AddCell(new Cell().Add(new Paragraph(movDate)).SetBackgroundColor(rowBg).SetPadding(4).SetFontSize(8.5f).SetTextAlignment(TextAlignment.CENTER));
                    
                    count++;
                }

                document.Add(table);

                // --- FOOTER ---
                document.Add(new Paragraph("\n"));
                Paragraph footerNote = new Paragraph($"Reporte de Auditoría de Inventario - {nombre} - Generado el {DateTime.Now:dd/MM/yyyy HH:mm}");
                footerNote.SetFontSize(7).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY);
                document.Add(footerNote);

                document.Close();
                Console.WriteLine("PDF: Detailed Horizontal Generation completed successfully.");
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            throw;
        }
    }

    private Cell CreateKpiCell(string title, string value, iText.Kernel.Colors.Color color, PdfFont font)
    {
        Paragraph pTitle = new Paragraph(title).SetFontSize(7).SetFont(font).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY);
        Paragraph pValue = new Paragraph(value).SetFontSize(12).SetFont(font).SetFontColor(color);
        
        return new Cell().Add(pTitle).Add(pValue).SetPadding(5).SetTextAlignment(TextAlignment.CENTER).SetBorder(new iText.Layout.Borders.SolidBorder(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY, 0.5f));
    }

    private Cell CreateStyledHeaderCell(string text, iText.Kernel.Colors.Color bg, PdfFont font)
    {
        Paragraph p = new Paragraph(text);
        p.SetFont(font);
        p.SetFontColor(iText.Kernel.Colors.ColorConstants.WHITE);
        return new Cell().Add(p).SetBackgroundColor(bg).SetPadding(5).SetFontSize(9);
    }
}




