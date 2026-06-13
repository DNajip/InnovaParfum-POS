using InnovaParfumPOS.Backend.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Barcodes;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace InnovaParfumPOS.Backend.Services;

public interface ILabelService
{
    byte[] GenerateLabelPdf(Producto product, string templateType, int quantity);
}

public class LabelService : ILabelService
{
    public byte[] GenerateLabelPdf(Producto product, string templateType, int quantity)
    {
        using (var stream = new MemoryStream())
        {
            var writer = new PdfWriter(stream);
            // Definir tamaño de página según plantilla (en mm, convertido a puntos: 1mm = 2.83465 pts)
            PageSize pageSize;
            switch (templateType.ToLower())
            {
                case "mini": // 80x20mm
                    pageSize = new PageSize(80 * 2.83465f, 20 * 2.83465f);
                    break;
                case "mediana": // 80x30mm
                    pageSize = new PageSize(80 * 2.83465f, 30 * 2.83465f);
                    break;
                case "grande": // 80x35mm
                    pageSize = new PageSize(80 * 2.83465f, 35 * 2.83465f);
                    break;
                default:
                    pageSize = new PageSize(80 * 2.83465f, 30 * 2.83465f);
                    break;
            }

            var pdf = new PdfDocument(writer);
            pdf.SetDefaultPageSize(pageSize);
            
            var document = new Document(pdf);
            document.SetMargins(2, 10, 2, 10); 

            PdfFont bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            for (int i = 0; i < quantity; i++)
            {
                // Crear página explícitamente para cada etiqueta
                pdf.AddNewPage(pageSize);
                
                // Mover el renderizado a la página recién creada
                RenderTemplate(document, pdf, product, templateType, bold, regular, pageSize, i + 1);
            }

            document.Close();
            return stream.ToArray();
        }
    }

    private void RenderTemplate(Document doc, PdfDocument pdf, Producto product, string templateType, PdfFont bold, PdfFont regular, PageSize pageSize, int pageNum)
    {
        float width = pageSize.GetWidth();
        float height = pageSize.GetHeight();

        if (templateType == "mini")
        {
            var namePara = new Paragraph(product.Nombre)
                .SetFont(bold)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(9)
                .SetMarginBottom(2);
            doc.Add(namePara);

            if (!string.IsNullOrEmpty(product.CodigoBarras))
            {
                Barcode128 barcode = new Barcode128(pdf);
                barcode.SetCodeType(Barcode128.CODE128);
                barcode.SetCode(product.CodigoBarras);
                barcode.SetFont(null); 
                
                Image barcodeImg = new Image(barcode.CreateFormXObject(iText.Kernel.Colors.ColorConstants.BLACK, iText.Kernel.Colors.ColorConstants.WHITE, pdf))
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                
                barcodeImg.SetWidth(width - 20); 
                barcodeImg.SetHeight(30f);
                doc.Add(barcodeImg);

                var codeNumPara = new Paragraph(product.CodigoBarras)
                    .SetFont(regular)
                    .SetFontSize(7)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFixedLeading(7)
                    .SetMarginTop(1);
                doc.Add(codeNumPara);
            }
        }
        else if (templateType == "mediana")
        {
            var namePara = new Paragraph(product.Nombre)
                .SetFont(bold)
                .SetFontSize(11)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(11)
                .SetMarginBottom(2);
            doc.Add(namePara);

            var pricePara = new Paragraph($"C$ {product.PrecioVenta:N2}")
                .SetFont(bold)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(14)
                .SetMarginBottom(2);
            doc.Add(pricePara);

            if (!string.IsNullOrEmpty(product.CodigoBarras))
            {
                Barcode128 barcode = new Barcode128(pdf);
                barcode.SetCodeType(Barcode128.CODE128);
                barcode.SetCode(product.CodigoBarras);
                barcode.SetFont(null); 
                
                Image barcodeImg = new Image(barcode.CreateFormXObject(iText.Kernel.Colors.ColorConstants.BLACK, iText.Kernel.Colors.ColorConstants.WHITE, pdf))
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                
                barcodeImg.SetWidth(width - 20);
                barcodeImg.SetHeight(38f);
                doc.Add(barcodeImg);

                var codeNumPara = new Paragraph(product.CodigoBarras)
                    .SetFont(regular)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFixedLeading(8)
                    .SetMarginTop(1);
                doc.Add(codeNumPara);
            }
        }
        else // grande
        {
            var namePara = new Paragraph(product.Nombre)
                .SetFont(bold)
                .SetFontSize(13)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(13)
                .SetMarginBottom(1);
            doc.Add(namePara);

            var metaPara = new Paragraph($"{product.IdGeneroNavigation?.DescGenero ?? "N/A"} | {product.IdConcentracionNavigation?.Nombre ?? "Gen"}")
                .SetFont(regular)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFixedLeading(8)
                .SetMarginBottom(3);
            doc.Add(metaPara);

            Table priceTable = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 })).UseAllAvailableWidth();
            priceTable.SetMarginBottom(3);
            priceTable.AddCell(new Cell().Add(new Paragraph("Precio:").SetFont(regular).SetFontSize(10).SetFixedLeading(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            priceTable.AddCell(new Cell().Add(new Paragraph($"C$ {product.PrecioVenta:N2}").SetFont(bold).SetFontSize(18).SetFixedLeading(18)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.BOTTOM));
            doc.Add(priceTable);

            if (!string.IsNullOrEmpty(product.CodigoBarras))
            {
                Barcode128 barcode = new Barcode128(pdf);
                barcode.SetCodeType(Barcode128.CODE128);
                barcode.SetCode(product.CodigoBarras);
                barcode.SetFont(null); 
                
                Image barcodeImg = new Image(barcode.CreateFormXObject(iText.Kernel.Colors.ColorConstants.BLACK, iText.Kernel.Colors.ColorConstants.WHITE, pdf))
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                
                barcodeImg.SetWidth(width - 20);
                barcodeImg.SetHeight(42f);
                doc.Add(barcodeImg);

                var codeNumPara = new Paragraph(product.CodigoBarras)
                    .SetFont(regular)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFixedLeading(9)
                    .SetMarginTop(1)
                    .SetMarginBottom(0);
                doc.Add(codeNumPara);
            }

            Table footerTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            footerTable.SetFixedPosition(10, 2, width - 20);
            footerTable.AddCell(new Cell().Add(new Paragraph("InnovaTec POS").SetFontSize(7).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            footerTable.AddCell(new Cell().Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy")).SetFontSize(7).SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT));
            doc.Add(footerTable);
        }
    }
}




