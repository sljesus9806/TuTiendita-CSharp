using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Generador de tickets de venta en formato PDF profesional
    /// </summary>
    public static class TicketPdfGenerator
    {
        static TicketPdfGenerator()
        {
            // Configurar licencia de QuestPDF (Community para proyectos no comerciales)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Genera un ticket de venta en PDF
        /// </summary>
        /// <param name="ventaId">ID de la venta</param>
        /// <param name="rutaArchivo">Ruta donde se guardará el PDF</param>
        /// <returns>True si se generó correctamente</returns>
        public static bool GenerarTicketVenta(int ventaId, string rutaArchivo)
        {
            try
            {
                var datosVenta = ObtenerDatosVenta(ventaId);
                if (datosVenta == null)
                {
                    return false;
                }

                var configuracion = ObtenerConfiguracion();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(new PageSize(80, 297, Unit.Millimetre)); // Tamaño ticket 80mm ancho
                        page.Margin(5, Unit.Millimetre);
                        page.DefaultTextStyle(x => x.FontSize(9));

                        page.Content().Column(column =>
                        {
                            column.Spacing(3);

                            // Encabezado con nombre de tienda
                            column.Item().AlignCenter().Text(configuracion.NombreTienda)
                                .FontSize(14).Bold();

                            if (!string.IsNullOrEmpty(configuracion.RUC))
                            {
                                column.Item().AlignCenter().Text($"RUC: {configuracion.RUC}")
                                    .FontSize(8);
                            }

                            if (!string.IsNullOrEmpty(configuracion.Direccion))
                            {
                                column.Item().AlignCenter().Text(configuracion.Direccion)
                                    .FontSize(8);
                            }

                            if (!string.IsNullOrEmpty(configuracion.Telefono))
                            {
                                column.Item().AlignCenter().Text($"Tel: {configuracion.Telefono}")
                                    .FontSize(8);
                            }

                            // Línea separadora
                            column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Información de la venta
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Ticket #: {ventaId}").FontSize(8);
                                row.RelativeItem().AlignRight().Text(datosVenta.Fecha).FontSize(8);
                            });

                            column.Item().Text($"Cajero: {datosVenta.NombreUsuario}").FontSize(8);
                            column.Item().Text($"Turno: #{datosVenta.TurnoId}").FontSize(8);

                            // Línea separadora
                            column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Tabla de productos
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Producto
                                    columns.RelativeColumn(1); // Cantidad
                                    columns.RelativeColumn(1.5f); // Precio
                                    columns.RelativeColumn(1.5f); // Total
                                });

                                // Encabezados
                                table.Header(header =>
                                {
                                    header.Cell().Text("Producto").FontSize(8).Bold();
                                    header.Cell().AlignCenter().Text("Cant").FontSize(8).Bold();
                                    header.Cell().AlignRight().Text("Precio").FontSize(8).Bold();
                                    header.Cell().AlignRight().Text("Total").FontSize(8).Bold();
                                });

                                // Productos
                                foreach (var producto in datosVenta.Productos)
                                {
                                    table.Cell().Text(producto.Nombre).FontSize(8);
                                    table.Cell().AlignCenter().Text(producto.Cantidad.ToString()).FontSize(8);
                                    table.Cell().AlignRight().Text($"${producto.Precio:N2}").FontSize(8);
                                    table.Cell().AlignRight().Text($"${producto.Total:N2}").FontSize(8);
                                }
                            });

                            // Línea separadora
                            column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Totales
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("SUBTOTAL:").FontSize(9);
                                row.RelativeItem().AlignRight().Text($"${datosVenta.Subtotal:N2}").FontSize(9);
                            });

                            if (configuracion.IVA > 0)
                            {
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"IVA ({configuracion.IVA}%):").FontSize(9);
                                    row.RelativeItem().AlignRight().Text($"${datosVenta.MontoIVA:N2}").FontSize(9);
                                });
                            }

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text("TOTAL:").FontSize(11).Bold();
                                row.RelativeItem().AlignRight().Text($"${datosVenta.Total:N2}").FontSize(11).Bold();
                            });

                            // Métodos de pago
                            if (datosVenta.MetodosPago.Count > 0)
                            {
                                column.Item().PaddingTop(3).Text("MÉTODOS DE PAGO:").FontSize(9).Bold();
                                foreach (var metodo in datosVenta.MetodosPago)
                                {
                                    column.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text($"  {metodo.Metodo}").FontSize(8);
                                        row.RelativeItem().AlignRight().Text($"${metodo.Monto:N2}").FontSize(8);
                                    });
                                }
                            }

                            // Línea separadora
                            column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                            // Mensaje de pie de página
                            if (!string.IsNullOrEmpty(configuracion.MensajePiePagina))
                            {
                                column.Item().AlignCenter().Text(configuracion.MensajePiePagina)
                                    .FontSize(8).Italic();
                            }

                            // Fecha de impresión
                            column.Item().AlignCenter().Text($"Impreso: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                .FontSize(7).FontColor(Colors.Grey.Darken2);
                        });
                    });
                })
                .GeneratePdf(rutaArchivo);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar ticket PDF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene los datos completos de una venta para generar el ticket
        /// </summary>
        private static DatosVenta ObtenerDatosVenta(int ventaId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    var datos = new DatosVenta();

                    // Obtener información de la venta
                    string queryVenta = @"SELECT v.Fecha, v.Total, v.TurnoId, u.Nombre
                                        FROM Ventas v
                                        LEFT JOIN Usuarios u ON v.UsuarioId = u.Id
                                        WHERE v.Id = @VentaId";

                    using (var cmd = new SQLiteCommand(queryVenta, connection))
                    {
                        cmd.Parameters.AddWithValue("@VentaId", ventaId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                return null;

                            datos.Fecha = reader.GetString(0);
                            datos.Total = reader.GetDecimal(1);
                            datos.TurnoId = reader.GetInt32(2);
                            datos.NombreUsuario = reader.IsDBNull(3) ? "Sistema" : reader.GetString(3);
                        }
                    }

                    // Obtener productos de la venta
                    string queryProductos = @"SELECT p.Nombre, dv.Cantidad, dv.PrecioUnitario,
                                            (dv.Cantidad * dv.PrecioUnitario) as Total
                                            FROM DetalleVenta dv
                                            INNER JOIN Productos p ON dv.ProductoId = p.Id
                                            WHERE dv.VentaId = @VentaId";

                    using (var cmd = new SQLiteCommand(queryProductos, connection))
                    {
                        cmd.Parameters.AddWithValue("@VentaId", ventaId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                datos.Productos.Add(new ProductoVenta
                                {
                                    Nombre = reader.GetString(0),
                                    Cantidad = reader.GetInt32(1),
                                    Precio = reader.GetDecimal(2),
                                    Total = reader.GetDecimal(3)
                                });
                            }
                        }
                    }

                    // Calcular subtotal e IVA
                    var config = ObtenerConfiguracion();
                    if (config.IVA > 0)
                    {
                        datos.Subtotal = datos.Total / (1m + (decimal)config.IVA / 100m);
                        datos.MontoIVA = datos.Total - datos.Subtotal;
                    }
                    else
                    {
                        datos.Subtotal = datos.Total;
                        datos.MontoIVA = 0;
                    }

                    // Obtener métodos de pago
                    datos.MetodosPago = ObtenerMetodosPagoVenta(ventaId, connection);

                    return datos;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener datos de venta: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene los métodos de pago utilizados en una venta
        /// </summary>
        private static List<MetodoPago> ObtenerMetodosPagoVenta(int ventaId, SQLiteConnection connection)
        {
            var metodos = new List<MetodoPago>();

            try
            {
                string query = @"SELECT MetodoPago, Monto
                               FROM PagosVenta
                               WHERE VentaId = @VentaId";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@VentaId", ventaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            metodos.Add(new MetodoPago
                            {
                                Metodo = reader.GetString(0),
                                Monto = reader.GetDecimal(1)
                            });
                        }
                    }
                }
            }
            catch
            {
                // Si la tabla PagosVenta no existe aún, inferir del total
                // (Para compatibilidad con versiones anteriores)
            }

            return metodos;
        }

        /// <summary>
        /// Obtiene la configuración de la tienda
        /// </summary>
        private static ConfiguracionTienda ObtenerConfiguracion()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT NombreTienda, RUC, Direccion, Telefono, Email,
                                   IVA, MensajePiePagina, MonedaSimbolo
                                   FROM Configuracion WHERE Id = 1";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ConfiguracionTienda
                                {
                                    NombreTienda = reader.GetString(0),
                                    RUC = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    Direccion = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Telefono = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    IVA = reader.GetDouble(5),
                                    MensajePiePagina = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    MonedaSimbolo = reader.IsDBNull(7) ? "$" : reader.GetString(7)
                                };
                            }
                        }
                    }
                }
            }
            catch { }

            // Configuración por defecto
            return new ConfiguracionTienda
            {
                NombreTienda = "TuTiendita",
                IVA = 0,
                MensajePiePagina = "Gracias por su compra",
                MonedaSimbolo = "$"
            };
        }

        /// <summary>
        /// Abre un archivo PDF con el visor predeterminado del sistema
        /// </summary>
        public static void AbrirPdf(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = rutaArchivo,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir PDF: {ex.Message}");
            }
        }
    }

    #region Clases de datos

    internal class DatosVenta
    {
        public string Fecha { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal Subtotal { get; set; }
        public decimal MontoIVA { get; set; }
        public int TurnoId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public List<ProductoVenta> Productos { get; set; } = new();
        public List<MetodoPago> MetodosPago { get; set; } = new();
    }

    internal class ProductoVenta
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get; set; }
    }

    internal class MetodoPago
    {
        public string Metodo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    private class ConfiguracionTienda
    {
        public string NombreTienda { get; set; } = string.Empty;
        public string RUC { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public double IVA { get; set; }
        public string MensajePiePagina { get; set; } = string.Empty;
        public string MonedaSimbolo { get; set; } = string.Empty;
    }

    #endregion
}
