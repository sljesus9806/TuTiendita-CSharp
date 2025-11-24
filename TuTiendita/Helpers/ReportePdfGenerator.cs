using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Generador de reportes en formato PDF profesional
    /// </summary>
    public static class ReportePdfGenerator
    {
        static ReportePdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera un reporte de cierre de caja en PDF
        /// </summary>
        public static bool GenerarReporteCierreCaja(int turnoId, string rutaArchivo)
        {
            try
            {
                var datosCierre = ObtenerDatosCierreCaja(turnoId);
                if (datosCierre == null)
                    return false;

                var config = ObtenerConfiguracion();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().Text(config.NombreTienda)
                                .FontSize(18).Bold();
                            column.Item().AlignCenter().Text("REPORTE DE CIERRE DE CAJA")
                                .FontSize(14).Bold();
                            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                        });

                        page.Content().PaddingTop(10).Column(column =>
                        {
                            // Información del turno
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Text($"Turno #: {turnoId}").Bold();
                                table.Cell().AlignRight().Text($"Cajero: {datosCierre.NombreCajero}");
                                table.Cell().Text($"Apertura: {datosCierre.FechaApertura}");
                                table.Cell().AlignRight().Text($"Cierre: {datosCierre.FechaCierre ?? "N/A"}");
                            });

                            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Resumen de ventas
                            column.Item().PaddingTop(10).Text("RESUMEN DE VENTAS").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Concepto").Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text("Monto").Bold();

                                AgregarFilaTabla(table, "Monto Inicial", datosCierre.MontoInicial);
                                AgregarFilaTabla(table, "Total Ventas", datosCierre.TotalVentas);
                                AgregarFilaTabla(table, $"Cantidad de Ventas", $"{datosCierre.CantidadVentas} tickets");
                            });

                            // Desglose por método de pago
                            column.Item().PaddingTop(15).Text("DESGLOSE POR MÉTODO DE PAGO").FontSize(12).Bold();
                            column.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text("Método").Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight().Text("Total").Bold();

                                AgregarFilaTabla(table, "Efectivo", datosCierre.TotalEfectivo);
                                AgregarFilaTabla(table, "Tarjeta", datosCierre.TotalTarjeta);
                                AgregarFilaTabla(table, "Transferencia", datosCierre.TotalTransferencia);

                                table.Cell().BorderTop(2).BorderColor(Colors.Grey.Darken2)
                                    .Padding(5).Text("TOTAL").Bold().FontSize(11);
                                table.Cell().BorderTop(2).BorderColor(Colors.Grey.Darken2)
                                    .Padding(5).AlignRight().Text($"${datosCierre.TotalGeneral:N2}").Bold().FontSize(11);
                            });

                            // Movimientos de caja
                            if (datosCierre.Movimientos.Count > 0)
                            {
                                column.Item().PaddingTop(15).Text("MOVIMIENTOS DE CAJA").FontSize(12).Bold();
                                column.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).Padding(5).Text("Fecha").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).Text("Tipo").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).Text("Concepto").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Monto").Bold();
                                    });

                                    foreach (var mov in datosCierre.Movimientos)
                                    {
                                        table.Cell().Padding(3).Text(mov.Fecha).FontSize(9);
                                        table.Cell().Padding(3).Text(mov.Tipo).FontSize(9);
                                        table.Cell().Padding(3).Text(mov.Concepto).FontSize(9);
                                        table.Cell().Padding(3).AlignRight()
                                            .Text($"${mov.Monto:N2}").FontSize(9)
                                            .FontColor(mov.Tipo == "Ingreso" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                    }
                                });
                            }

                            // Diferencia (si el turno está cerrado)
                            if (datosCierre.FechaCierre != null && datosCierre.DiferenciaEfectivo.HasValue)
                            {
                                column.Item().PaddingTop(15).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    AgregarFilaTabla(table, "Efectivo Esperado", datosCierre.EfectivoEsperado);
                                    AgregarFilaTabla(table, "Efectivo Contado", datosCierre.EfectivoContado);

                                    var diferencia = datosCierre.DiferenciaEfectivo.Value;
                                    var colorDiferencia = diferencia == 0 ? Colors.Green.Darken2 :
                                                        diferencia > 0 ? Colors.Blue.Darken2 : Colors.Red.Darken2;

                                    table.Cell().BorderTop(2).BorderColor(Colors.Grey.Darken2)
                                        .Padding(5).Text("DIFERENCIA").Bold().FontSize(11);
                                    table.Cell().BorderTop(2).BorderColor(Colors.Grey.Darken2)
                                        .Padding(5).AlignRight()
                                        .Text($"${Math.Abs(diferencia):N2} {(diferencia > 0 ? "(sobrante)" : diferencia < 0 ? "(faltante)" : "")}")
                                        .Bold().FontSize(11).FontColor(colorDiferencia);
                                });

                                if (!string.IsNullOrEmpty(datosCierre.Notas))
                                {
                                    column.Item().PaddingTop(10).Text("NOTAS:").Bold();
                                    column.Item().PaddingTop(3).Border(1).BorderColor(Colors.Grey.Lighten2)
                                        .Padding(8).Text(datosCierre.Notas).FontSize(9);
                                }
                            }
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Generado el: ").FontSize(8);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(8).Bold();
                            text.Span(" | Página ").FontSize(8);
                            text.CurrentPageNumber().FontSize(8);
                        });
                    });
                })
                .GeneratePdf(rutaArchivo);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar reporte de cierre: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Genera un reporte de ventas por período en PDF
        /// </summary>
        public static bool GenerarReporteVentas(DateTime fechaInicio, DateTime fechaFin, string rutaArchivo)
        {
            try
            {
                var datosVentas = ObtenerDatosVentas(fechaInicio, fechaFin);
                var config = ObtenerConfiguracion();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Column(column =>
                        {
                            column.Item().AlignCenter().Text(config.NombreTienda)
                                .FontSize(18).Bold();
                            column.Item().AlignCenter().Text("REPORTE DE VENTAS")
                                .FontSize(14).Bold();
                            column.Item().AlignCenter().Text($"Período: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}")
                                .FontSize(10);
                            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                        });

                        page.Content().PaddingTop(10).Column(column =>
                        {
                            // Resumen general
                            column.Item().Background(Colors.Blue.Lighten4).Padding(10).Column(summary =>
                            {
                                summary.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Total Ventas: {datosVentas.TotalVentas}")
                                        .Bold().FontSize(11);
                                    row.RelativeItem().AlignRight().Text($"Monto Total: ${datosVentas.MontoTotal:N2}")
                                        .Bold().FontSize(11);
                                });
                                summary.Item().PaddingTop(3).Text($"Ticket Promedio: ${datosVentas.TicketPromedio:N2}");
                            });

                            // Ventas por cajero
                            if (datosVentas.VentasPorCajero.Count > 0)
                            {
                                column.Item().PaddingTop(15).Text("VENTAS POR CAJERO").FontSize(12).Bold();
                                column.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).Padding(5).Text("Cajero").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Cant.").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Total").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Promedio").Bold();
                                    });

                                    foreach (var cajero in datosVentas.VentasPorCajero)
                                    {
                                        table.Cell().Padding(3).Text(cajero.Nombre);
                                        table.Cell().Padding(3).AlignRight().Text(cajero.Cantidad.ToString());
                                        table.Cell().Padding(3).AlignRight().Text($"${cajero.Total:N2}");
                                        table.Cell().Padding(3).AlignRight().Text($"${cajero.Promedio:N2}");
                                    }
                                });
                            }

                            // Productos más vendidos
                            if (datosVentas.ProductosMasVendidos.Count > 0)
                            {
                                column.Item().PaddingTop(15).Text("PRODUCTOS MÁS VENDIDOS (TOP 10)").FontSize(12).Bold();
                                column.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(4);
                                        columns.RelativeColumn(1);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).Padding(5).Text("Producto").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Cantidad").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).AlignRight().Text("Total").Bold();
                                    });

                                    foreach (var producto in datosVentas.ProductosMasVendidos.Take(10))
                                    {
                                        table.Cell().Padding(3).Text(producto.Nombre);
                                        table.Cell().Padding(3).AlignRight().Text(producto.Cantidad.ToString());
                                        table.Cell().Padding(3).AlignRight().Text($"${producto.Total:N2}");
                                    }
                                });
                            }
                        });

                        page.Footer().AlignCenter().Text(text =>
                        {
                            text.Span("Generado el: ").FontSize(8);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(8).Bold();
                            text.Span(" | Página ").FontSize(8);
                            text.CurrentPageNumber().FontSize(8);
                        });
                    });
                })
                .GeneratePdf(rutaArchivo);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar reporte de ventas: {ex.Message}");
                return false;
            }
        }

        #region Métodos auxiliares

        private static void AgregarFilaTabla(IContainer table, string concepto, decimal monto)
        {
            table.Cell().Padding(5).Text(concepto);
            table.Cell().Padding(5).AlignRight().Text($"${monto:N2}");
        }

        private static void AgregarFilaTabla(IContainer table, string concepto, string valor)
        {
            table.Cell().Padding(5).Text(concepto);
            table.Cell().Padding(5).AlignRight().Text(valor);
        }

        private static DatosCierreCaja ObtenerDatosCierreCaja(int turnoId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    var datos = new DatosCierreCaja { Movimientos = new List<MovimientoCaja>() };

                    // Obtener información del turno
                    string queryTurno = @"SELECT t.FechaApertura, t.FechaCierre, t.MontoInicial,
                                        t.TotalEfectivo, t.TotalTarjeta, t.TotalTransferencia,
                                        t.EfectivoEsperado, t.EfectivoContado, t.Diferencia, t.Notas,
                                        u.Nombre
                                        FROM Turnos t
                                        LEFT JOIN Usuarios u ON t.UsuarioId = u.Id
                                        WHERE t.Id = @TurnoId";

                    using (var cmd = new SQLiteCommand(queryTurno, connection))
                    {
                        cmd.Parameters.AddWithValue("@TurnoId", turnoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                return null;

                            datos.FechaApertura = reader.GetString(0);
                            datos.FechaCierre = reader.IsDBNull(1) ? null : reader.GetString(1);
                            datos.MontoInicial = reader.GetDecimal(2);
                            datos.TotalEfectivo = !reader.IsDBNull(3) ? reader.GetDecimal(3) : 0;
                            datos.TotalTarjeta = !reader.IsDBNull(4) ? reader.GetDecimal(4) : 0;
                            datos.TotalTransferencia = !reader.IsDBNull(5) ? reader.GetDecimal(5) : 0;
                            datos.EfectivoEsperado = !reader.IsDBNull(6) ? reader.GetDecimal(6) : 0;
                            datos.EfectivoContado = !reader.IsDBNull(7) ? reader.GetDecimal(7) : 0;
                            datos.DiferenciaEfectivo = !reader.IsDBNull(8) ? (decimal?)reader.GetDecimal(8) : null;
                            datos.Notas = reader.IsDBNull(9) ? null : reader.GetString(9);
                            datos.NombreCajero = reader.IsDBNull(10) ? "Sistema" : reader.GetString(10);
                        }
                    }

                    // Calcular totales
                    datos.TotalGeneral = datos.TotalEfectivo + datos.TotalTarjeta + datos.TotalTransferencia;
                    datos.TotalVentas = datos.TotalGeneral - datos.MontoInicial;

                    // Obtener cantidad de ventas
                    string queryVentas = "SELECT COUNT(*) FROM Ventas WHERE TurnoId = @TurnoId";
                    using (var cmd = new SQLiteCommand(queryVentas, connection))
                    {
                        cmd.Parameters.AddWithValue("@TurnoId", turnoId);
                        datos.CantidadVentas = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Obtener movimientos de caja
                    string queryMovimientos = @"SELECT Fecha, Tipo, Concepto, Monto
                                              FROM MovimientosCaja
                                              WHERE TurnoId = @TurnoId
                                              ORDER BY Fecha";

                    using (var cmd = new SQLiteCommand(queryMovimientos, connection))
                    {
                        cmd.Parameters.AddWithValue("@TurnoId", turnoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                datos.Movimientos.Add(new MovimientoCaja
                                {
                                    Fecha = reader.GetString(0),
                                    Tipo = reader.GetString(1),
                                    Concepto = reader.GetString(2),
                                    Monto = reader.GetDecimal(3)
                                });
                            }
                        }
                    }

                    return datos;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener datos de cierre: {ex.Message}");
                return null;
            }
        }

        private static DatosReporteVentas ObtenerDatosVentas(DateTime fechaInicio, DateTime fechaFin)
        {
            var datos = new DatosReporteVentas
            {
                VentasPorCajero = new List<VentaPorCajero>(),
                ProductosMasVendidos = new List<ProductoVendido>()
            };

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    string fechaInicioStr = fechaInicio.ToString("yyyy-MM-dd 00:00:00");
                    string fechaFinStr = fechaFin.ToString("yyyy-MM-dd 23:59:59");

                    // Totales generales
                    string queryTotales = @"SELECT COUNT(*), COALESCE(SUM(Total), 0)
                                          FROM Ventas
                                          WHERE Fecha >= @FechaInicio AND Fecha <= @FechaFin";

                    using (var cmd = new SQLiteCommand(queryTotales, connection))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicioStr);
                        cmd.Parameters.AddWithValue("@FechaFin", fechaFinStr);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                datos.TotalVentas = reader.GetInt32(0);
                                datos.MontoTotal = reader.GetDecimal(1);
                                datos.TicketPromedio = datos.TotalVentas > 0 ? datos.MontoTotal / datos.TotalVentas : 0;
                            }
                        }
                    }

                    // Ventas por cajero
                    string queryPorCajero = @"SELECT u.Nombre, COUNT(*) as Cantidad,
                                            COALESCE(SUM(v.Total), 0) as Total
                                            FROM Ventas v
                                            LEFT JOIN Usuarios u ON v.UsuarioId = u.Id
                                            WHERE v.Fecha >= @FechaInicio AND v.Fecha <= @FechaFin
                                            GROUP BY u.Nombre
                                            ORDER BY Total DESC";

                    using (var cmd = new SQLiteCommand(queryPorCajero, connection))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicioStr);
                        cmd.Parameters.AddWithValue("@FechaFin", fechaFinStr);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var cantidad = reader.GetInt32(1);
                                var total = reader.GetDecimal(2);
                                datos.VentasPorCajero.Add(new VentaPorCajero
                                {
                                    Nombre = reader.IsDBNull(0) ? "Sistema" : reader.GetString(0),
                                    Cantidad = cantidad,
                                    Total = total,
                                    Promedio = cantidad > 0 ? total / cantidad : 0
                                });
                            }
                        }
                    }

                    // Productos más vendidos
                    string queryProductos = @"SELECT p.Nombre,
                                            SUM(dv.Cantidad) as CantidadVendida,
                                            SUM(dv.Cantidad * dv.PrecioUnitario) as TotalVendido
                                            FROM DetalleVenta dv
                                            INNER JOIN Productos p ON dv.ProductoId = p.Id
                                            INNER JOIN Ventas v ON dv.VentaId = v.Id
                                            WHERE v.Fecha >= @FechaInicio AND v.Fecha <= @FechaFin
                                            GROUP BY p.Nombre
                                            ORDER BY CantidadVendida DESC";

                    using (var cmd = new SQLiteCommand(queryProductos, connection))
                    {
                        cmd.Parameters.AddWithValue("@FechaInicio", fechaInicioStr);
                        cmd.Parameters.AddWithValue("@FechaFin", fechaFinStr);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                datos.ProductosMasVendidos.Add(new ProductoVendido
                                {
                                    Nombre = reader.GetString(0),
                                    Cantidad = reader.GetInt32(1),
                                    Total = reader.GetDecimal(2)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener datos de ventas: {ex.Message}");
            }

            return datos;
        }

        private static ConfiguracionTienda ObtenerConfiguracion()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT NombreTienda FROM Configuracion WHERE Id = 1";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ConfiguracionTienda { NombreTienda = reader.GetString(0) };
                            }
                        }
                    }
                }
            }
            catch { }

            return new ConfiguracionTienda { NombreTienda = "TuTiendita" };
        }

        #endregion
    }

    #region Clases de datos para reportes

    internal class DatosCierreCaja
    {
        public string FechaApertura { get; set; }
        public string FechaCierre { get; set; }
        public string NombreCajero { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }
        public decimal TotalGeneral { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public decimal EfectivoEsperado { get; set; }
        public decimal EfectivoContado { get; set; }
        public decimal? DiferenciaEfectivo { get; set; }
        public string Notas { get; set; }
        public List<MovimientoCaja> Movimientos { get; set; }
    }

    internal class MovimientoCaja
    {
        public string Fecha { get; set; }
        public string Tipo { get; set; }
        public string Concepto { get; set; }
        public decimal Monto { get; set; }
    }

    internal class DatosReporteVentas
    {
        public int TotalVentas { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<VentaPorCajero> VentasPorCajero { get; set; }
        public List<ProductoVendido> ProductosMasVendidos { get; set; }
    }

    internal class VentaPorCajero
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public decimal Promedio { get; set; }
    }

    internal class ProductoVendido
    {
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    file class ConfiguracionTienda
    {
        public string NombreTienda { get; set; }
    }

    #endregion
}
