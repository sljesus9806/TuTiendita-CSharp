using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Generador de PDF para representación impresa de CFDI
    /// </summary>
    public static class CFDIPdfGenerator
    {
        static CFDIPdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void GenerarPDF(CFDI cfdi, ConfiguracionFiscal config, string rutaArchivo)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, cfdi, config));
                    page.Content().Element(c => ComposeContent(c, cfdi));
                    page.Footer().Element(c => ComposeFooter(c, cfdi));
                });
            });

            documento.GeneratePdf(rutaArchivo);
        }

        private static void ComposeHeader(IContainer container, CFDI cfdi, ConfiguracionFiscal config)
        {
            container.Column(column =>
            {
                // Encabezado con logo y datos del emisor
                column.Item().Row(row =>
                {
                    // Logo (si existe)
                    row.ConstantItem(80).Height(60).AlignMiddle().AlignCenter()
                        .Border(1).BorderColor(Colors.Grey.Lighten2)
                        .Text("LOGO").FontSize(8).FontColor(Colors.Grey.Medium);

                    row.RelativeItem().PaddingLeft(15).Column(col =>
                    {
                        col.Item().Text(config?.RazonSocial ?? cfdi.EmisorNombre)
                            .Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"RFC: {cfdi.EmisorRFC}").FontSize(10);
                        col.Item().Text($"Régimen Fiscal: {cfdi.EmisorRegimenFiscal} - {CatalogosSAT.GetRegimenFiscalPorClave(cfdi.EmisorRegimenFiscal)?.Descripcion ?? ""}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        if (config != null && !string.IsNullOrEmpty(config.DireccionCompleta))
                        {
                            col.Item().Text(config.DireccionCompleta).FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    row.ConstantItem(180).Column(col =>
                    {
                        col.Item().Background(Colors.Blue.Darken2).Padding(8)
                            .Text("FACTURA").Bold().FontSize(14).FontColor(Colors.White).AlignCenter();

                        col.Item().Border(1).BorderColor(Colors.Blue.Darken2).Padding(5).Column(innerCol =>
                        {
                            innerCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Serie:").FontSize(9);
                                r.RelativeItem().Text(cfdi.Serie ?? "").Bold().FontSize(9).AlignRight();
                            });
                            innerCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Folio:").FontSize(9);
                                r.RelativeItem().Text(cfdi.Folio).Bold().FontSize(9).AlignRight();
                            });
                            innerCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Fecha:").FontSize(9);
                                r.RelativeItem().Text(cfdi.Fecha).FontSize(8).AlignRight();
                            });
                        });
                    });
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Datos del receptor
                column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(col =>
                {
                    col.Item().Text("RECEPTOR").Bold().FontSize(10).FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"RFC: {cfdi.ReceptorRFC}").FontSize(10);
                            c.Item().Text($"Nombre: {cfdi.ReceptorNombre}").FontSize(10);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Uso CFDI: {cfdi.ReceptorUsoCFDI} - {cfdi.UsoCFDIDescripcion}").FontSize(9);
                            c.Item().Text($"Domicilio Fiscal: {cfdi.ReceptorDomicilioFiscalCP}").FontSize(9);
                        });
                    });
                });

                column.Item().PaddingVertical(5);

                // Datos del comprobante
                column.Item().Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("Tipo de Comprobante").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(cfdi.TipoComprobanteDescripcion).Bold().FontSize(10);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("Forma de Pago").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{cfdi.FormaPagoClave} - {cfdi.FormaPagoDescripcion}").FontSize(9);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("Método de Pago").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"{cfdi.MetodoPagoClave} - {cfdi.MetodoPagoDescripcion}").FontSize(9);
                    });
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("Moneda").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(cfdi.Moneda).Bold().FontSize(10);
                    });
                });

                column.Item().PaddingVertical(10);
            });
        }

        private static void ComposeContent(IContainer container, CFDI cfdi)
        {
            container.Column(column =>
            {
                // Tabla de conceptos
                column.Item().Text("CONCEPTOS").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                column.Item().PaddingTop(5);

                column.Item().Table(table =>
                {
                    // Definir columnas
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);  // Cantidad
                        columns.ConstantColumn(80);  // Clave SAT
                        columns.RelativeColumn();    // Descripción
                        columns.ConstantColumn(80);  // P. Unitario
                        columns.ConstantColumn(80);  // Importe
                    });

                    // Encabezado
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("Cant.").Bold().FontSize(9).FontColor(Colors.White).AlignCenter();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("Clave SAT").Bold().FontSize(9).FontColor(Colors.White).AlignCenter();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("Descripción").Bold().FontSize(9).FontColor(Colors.White);
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("P. Unitario").Bold().FontSize(9).FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                            .Text("Importe").Bold().FontSize(9).FontColor(Colors.White).AlignRight();
                    });

                    // Filas
                    foreach (var concepto in cfdi.Conceptos)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text(concepto.Cantidad.ToString("N2")).FontSize(9).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text(concepto.ClaveProdServ).FontSize(8).AlignCenter();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text(concepto.Descripcion).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text(concepto.ValorUnitario.ToString("C")).FontSize(9).AlignRight();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text(concepto.Importe.ToString("C")).FontSize(9).AlignRight();
                    }
                });

                column.Item().PaddingVertical(15);

                // Totales
                column.Item().Row(row =>
                {
                    row.RelativeItem(); // Espacio vacío

                    row.ConstantItem(250).Column(totalsCol =>
                    {
                        // Subtotal
                        totalsCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotal:").FontSize(10);
                            r.ConstantItem(100).Text(cfdi.Subtotal.ToString("C")).FontSize(10).AlignRight();
                        });

                        // IVA
                        totalsCol.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text("IVA Trasladado (16%):").FontSize(10);
                            r.ConstantItem(100).Text(cfdi.IVATrasladado.ToString("C")).FontSize(10).AlignRight();
                        });

                        // Descuento (si aplica)
                        if (cfdi.Descuento > 0)
                        {
                            totalsCol.Item().PaddingTop(3).Row(r =>
                            {
                                r.RelativeItem().Text("Descuento:").FontSize(10);
                                r.ConstantItem(100).Text($"-{cfdi.Descuento:C}").FontSize(10).AlignRight();
                            });
                        }

                        // Línea separadora
                        totalsCol.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

                        // Total
                        totalsCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text("TOTAL:").Bold().FontSize(12);
                            r.ConstantItem(100).Text(cfdi.Total.ToString("C")).Bold().FontSize(12).AlignRight()
                                .FontColor(Colors.Blue.Darken2);
                        });
                    });
                });

                column.Item().PaddingVertical(15);

                // Información de timbrado (si existe)
                if (!string.IsNullOrEmpty(cfdi.UUID))
                {
                    column.Item().Background(Colors.Green.Lighten5).Border(1).BorderColor(Colors.Green.Darken1)
                        .Padding(10).Column(timbreCol =>
                        {
                            timbreCol.Item().Text("INFORMACIÓN DEL TIMBRE FISCAL DIGITAL")
                                .Bold().FontSize(10).FontColor(Colors.Green.Darken2);

                            timbreCol.Item().PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("UUID (Folio Fiscal):").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    c.Item().Text(cfdi.UUID).Bold().FontSize(9);
                                });
                            });

                            if (!string.IsNullOrEmpty(cfdi.FechaTimbrado))
                            {
                                timbreCol.Item().PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Fecha de Timbrado:").FontSize(8).FontColor(Colors.Grey.Darken1);
                                        c.Item().Text(cfdi.FechaTimbrado).FontSize(9);
                                    });
                                });
                            }

                            if (!string.IsNullOrEmpty(cfdi.NoCertificadoSAT))
                            {
                                timbreCol.Item().PaddingTop(3).Text($"No. Certificado SAT: {cfdi.NoCertificadoSAT}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            }
                        });
                }
                else
                {
                    // Factura sin timbrar
                    column.Item().Background(Colors.Orange.Lighten5).Border(1).BorderColor(Colors.Orange.Darken1)
                        .Padding(10).Column(pendCol =>
                        {
                            pendCol.Item().Text("COMPROBANTE PENDIENTE DE TIMBRADO")
                                .Bold().FontSize(10).FontColor(Colors.Orange.Darken2).AlignCenter();
                            pendCol.Item().PaddingTop(5)
                                .Text("Este documento no tiene validez fiscal hasta que sea timbrado por un PAC autorizado.")
                                .FontSize(9).FontColor(Colors.Grey.Darken1).AlignCenter();
                        });
                }
            });
        }

        private static void ComposeFooter(IContainer container, CFDI cfdi)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Este documento es una representación impresa de un CFDI")
                            .FontSize(8).FontColor(Colors.Grey.Medium);

                        if (!string.IsNullOrEmpty(cfdi.UUID))
                        {
                            c.Item().Text("Puede verificar la autenticidad en: https://verificacfdi.facturaelectronica.sat.gob.mx/")
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                        }
                    });

                    row.ConstantItem(150).AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        }

        /// <summary>
        /// Genera el PDF como un arreglo de bytes (útil para previsualización)
        /// </summary>
        public static byte[] GenerarPDFBytes(CFDI cfdi, ConfiguracionFiscal config)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, cfdi, config));
                    page.Content().Element(c => ComposeContent(c, cfdi));
                    page.Footer().Element(c => ComposeFooter(c, cfdi));
                });
            });

            return documento.GeneratePdf();
        }
    }
}
