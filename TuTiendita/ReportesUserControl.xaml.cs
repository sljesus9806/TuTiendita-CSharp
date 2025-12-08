using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static TuTiendita.ProductosUserControl;

namespace TuTiendita
{
    public partial class ReportesUserControl : UserControl
    {
        private Usuario usuarioActual;

        public ReportesUserControl() : this(null)
        {
        }

        public ReportesUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            InicializarFechas();
            CargarReporteInventario();
        }

        private void InicializarFechas()
        {
            dpFechaDesde.SelectedDate = DateTime.Today;
            dpFechaHasta.SelectedDate = DateTime.Today;
        }

        #region Sales Report Methods

        private void BtnHoy_Click(object sender, RoutedEventArgs e)
        {
            dpFechaDesde.SelectedDate = DateTime.Today;
            dpFechaHasta.SelectedDate = DateTime.Today;
            CargarReporteVentas();
        }

        private void BtnSemana_Click(object sender, RoutedEventArgs e)
        {
            dpFechaDesde.SelectedDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            dpFechaHasta.SelectedDate = DateTime.Today;
            CargarReporteVentas();
        }

        private void BtnMes_Click(object sender, RoutedEventArgs e)
        {
            dpFechaDesde.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            dpFechaHasta.SelectedDate = DateTime.Today;
            CargarReporteVentas();
        }

        private void BtnBuscarVentas_Click(object sender, RoutedEventArgs e)
        {
            CargarReporteVentas();
        }

        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFechaDesde.SelectedDate.HasValue || !dpFechaHasta.SelectedDate.HasValue)
            {
                MessageBox.Show("Seleccione las fechas de inicio y fin para generar el reporte.", "Advertencia",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Crear directorio de reportes si no existe
                string reportesFolder = "Reportes";
                if (!System.IO.Directory.Exists(reportesFolder))
                {
                    System.IO.Directory.CreateDirectory(reportesFolder);
                }

                // Generar nombre de archivo PDF
                DateTime fechaInicio = dpFechaDesde.SelectedDate.Value;
                DateTime fechaFin = dpFechaHasta.SelectedDate.Value;
                string fileName = $"Reportes/ReporteVentas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}_{DateTime.Now:HHmmss}.pdf";

                // Usar el generador de PDF profesional
                bool exito = Helpers.ReportePdfGenerator.GenerarReporteVentas(fechaInicio, fechaFin, fileName);

                if (exito)
                {
                    // Registrar en auditoría
                    if (usuarioActual != null)
                    {
                        Helpers.AuditLogger.RegistrarReporteGenerado(usuarioActual, "Reporte de Ventas", fileName);
                    }

                    // Preguntar si desea abrir el reporte
                    var resultado = MessageBox.Show(
                        "Reporte de ventas exportado exitosamente a PDF\n\n¿Desea abrir el reporte?",
                        "Exportación Exitosa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        Helpers.TicketPdfGenerator.AbrirPdf(fileName);
                    }
                }
                else
                {
                    MessageBox.Show("Error al generar el reporte PDF", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar reporte: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarReporteVentas()
        {
            if (!dpFechaDesde.SelectedDate.HasValue || !dpFechaHasta.SelectedDate.HasValue)
            {
                MessageBox.Show("Seleccione las fechas de inicio y fin.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ventas = new List<Venta>();
                decimal totalVentas = 0;
                decimal totalVentasCanceladas = 0;
                int totalProductos = 0;

                string fechaDesde = dpFechaDesde.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00");
                string fechaHasta = dpFechaHasta.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59");

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Get sales with Estado column
                    string query = @"SELECT Id, TurnoId, UsuarioId, UsuarioNombre, Fecha, Total, MontoPagado, Cambio,
                                           COALESCE(Estado, 'Completada') as Estado,
                                           MotivoCancelacion, CanceladoPor, FechaCancelacion
                                   FROM Ventas
                                   WHERE Fecha BETWEEN @fechaDesde AND @fechaHasta
                                   ORDER BY Fecha DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde);
                        cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var venta = new Venta
                                {
                                    Id = reader.GetInt32(0),
                                    TurnoId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    UsuarioId = reader.GetInt32(2),
                                    UsuarioNombre = reader.GetString(3),
                                    Fecha = reader.GetString(4),
                                    Total = reader.GetDecimal(5),
                                    MontoPagado = reader.GetDecimal(6),
                                    Cambio = reader.GetDecimal(7),
                                    Estado = reader.IsDBNull(8) ? "Completada" : reader.GetString(8),
                                    MotivoCancelacion = reader.IsDBNull(9) ? null : reader.GetString(9),
                                    CanceladoPor = reader.IsDBNull(10) ? null : reader.GetString(10),
                                    FechaCancelacion = reader.IsDBNull(11) ? null : reader.GetString(11)
                                };

                                ventas.Add(venta);

                                // Solo sumar ventas completadas al total
                                if (venta.Estado == "Completada")
                                {
                                    totalVentas += venta.Total;
                                }
                                else
                                {
                                    totalVentasCanceladas += venta.Total;
                                }
                            }
                        }
                    }

                    // Get total products sold (solo de ventas completadas)
                    string productsQuery = @"SELECT SUM(dv.Cantidad)
                                           FROM DetalleVentas dv
                                           INNER JOIN Ventas v ON dv.VentaId = v.Id
                                           WHERE v.Fecha BETWEEN @fechaDesde AND @fechaHasta
                                           AND COALESCE(v.Estado, 'Completada') = 'Completada'";

                    using (var cmd = new SQLiteCommand(productsQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde);
                        cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta);
                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            totalProductos = Convert.ToInt32(result);
                        }
                    }
                }

                // Update UI
                dgVentas.ItemsSource = ventas;

                int ventasCompletadas = ventas.Count(v => v.Estado == "Completada");
                txtTotalVentas.Text = totalVentas.ToString("C");
                txtNumTransacciones.Text = $"{ventasCompletadas} ({ventas.Count(v => v.Estado == "Cancelada")} canceladas)";
                txtVentaPromedio.Text = ventasCompletadas > 0 ? (totalVentas / ventasCompletadas).ToString("C") : "$0.00";
                txtProductosVendidos.Text = totalProductos.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reporte de ventas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVentas.SelectedItem is Venta venta)
            {
                CargarDetalleVenta(venta);
            }
        }

        private void CargarDetalleVenta(Venta venta)
        {
            try
            {
                var detalles = new List<DetalleVenta>();

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM DetalleVentas WHERE VentaId = @ventaId";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ventaId", venta.Id);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                detalles.Add(new DetalleVenta
                                {
                                    Id = reader.GetInt32(0),
                                    VentaId = reader.GetInt32(1),
                                    ProductoCodigo = reader.GetString(2),
                                    ProductoNombre = reader.GetString(3),
                                    Cantidad = reader.GetInt32(4),
                                    PrecioUnitario = reader.GetDecimal(5),
                                    Subtotal = reader.GetDecimal(6)
                                });
                            }
                        }
                    }
                }

                // Update UI
                txtNoSeleccion.Visibility = Visibility.Collapsed;
                pnlDetalleVenta.Visibility = Visibility.Visible;

                txtDetalleVentaId.Text = venta.Id.ToString();
                txtDetalleFecha.Text = venta.Fecha;
                txtDetalleUsuario.Text = venta.UsuarioNombre;
                txtDetalleTotal.Text = venta.Total.ToString("C");
                txtDetallePagado.Text = venta.MontoPagado.ToString("C");
                txtDetalleCambio.Text = venta.Cambio.ToString("C");

                // Mostrar información de cancelación si aplica
                if (venta.Estado == "Cancelada")
                {
                    pnlInfoCancelacion.Visibility = Visibility.Visible;
                    txtCanceladoPor.Text = venta.CanceladoPor ?? "N/A";
                    txtFechaCancelacion.Text = venta.FechaCancelacion ?? "N/A";
                    txtMotivoCancelacion.Text = venta.MotivoCancelacion ?? "Sin motivo especificado";
                }
                else
                {
                    pnlInfoCancelacion.Visibility = Visibility.Collapsed;
                }

                dgDetalleVenta.ItemsSource = detalles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle de venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (usuarioActual == null)
            {
                MessageBox.Show("No se puede identificar el usuario actual.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sender is Button button && button.Tag is int ventaId)
            {
                // Buscar la venta en la lista actual
                var ventas = dgVentas.ItemsSource as List<Venta>;
                var venta = ventas?.FirstOrDefault(v => v.Id == ventaId);

                if (venta == null)
                {
                    MessageBox.Show("No se encontró la venta.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (venta.Estado == "Cancelada")
                {
                    MessageBox.Show("Esta venta ya fue cancelada.", "Advertencia",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Solicitar motivo de cancelación
                var dialogoMotivo = new DialogoEntrada("Ingrese el motivo de la cancelación:");
                if (dialogoMotivo.ShowDialog() != true || string.IsNullOrWhiteSpace(dialogoMotivo.InputText))
                {
                    MessageBox.Show("Debe ingresar un motivo para cancelar la venta.", "Advertencia",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string motivo = dialogoMotivo.InputText;

                // Confirmar cancelación
                var resultado = MessageBox.Show(
                    $"¿Está seguro de que desea cancelar la venta #{ventaId}?\n\n" +
                    $"Total: {venta.Total:C}\n" +
                    $"Fecha: {venta.Fecha}\n" +
                    $"Vendedor: {venta.UsuarioNombre}\n\n" +
                    $"Motivo: {motivo}\n\n" +
                    "ADVERTENCIA: Esta acción restaurará el stock de los productos y quedará registrada en el log de auditoría.",
                    "Confirmar Cancelación de Venta",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado != MessageBoxResult.Yes)
                {
                    return;
                }

                // Proceder con la cancelación
                try
                {
                    CancelarVenta(ventaId, motivo, venta);
                    MessageBox.Show($"Venta #{ventaId} cancelada exitosamente.\nEl stock de los productos ha sido restaurado.",
                        "Cancelación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarReporteVentas();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cancelar la venta: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelarVenta(int ventaId, string motivo, Venta venta)
        {
            using (var connection = Database.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Obtener los detalles de la venta para restaurar stock
                        var detalles = new List<(string codigo, int cantidad)>();
                        string queryDetalles = "SELECT ProductoCodigo, Cantidad FROM DetalleVentas WHERE VentaId = @ventaId";
                        using (var cmd = new SQLiteCommand(queryDetalles, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ventaId", ventaId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    detalles.Add((reader.GetString(0), reader.GetInt32(1)));
                                }
                            }
                        }

                        // 2. Restaurar el stock de cada producto
                        foreach (var (codigo, cantidad) in detalles)
                        {
                            string queryStock = "UPDATE Productos SET Stock = Stock + @cantidad WHERE Codigo = @codigo";
                            using (var cmd = new SQLiteCommand(queryStock, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@cantidad", cantidad);
                                cmd.Parameters.AddWithValue("@codigo", codigo);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 3. Actualizar la venta con el estado de cancelación
                        string queryUpdate = @"UPDATE Ventas
                                             SET Estado = 'Cancelada',
                                                 MotivoCancelacion = @motivo,
                                                 CanceladoPor = @canceladoPor,
                                                 FechaCancelacion = @fechaCancelacion
                                             WHERE Id = @ventaId";
                        using (var cmd = new SQLiteCommand(queryUpdate, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@motivo", motivo);
                            cmd.Parameters.AddWithValue("@canceladoPor", usuarioActual.Nombre);
                            cmd.Parameters.AddWithValue("@fechaCancelacion", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@ventaId", ventaId);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Si la venta tenía turno asociado, actualizar los totales del turno
                        if (venta.TurnoId.HasValue)
                        {
                            string queryTurno = @"UPDATE Turnos
                                                SET TotalVentas = TotalVentas - @total
                                                WHERE Id = @turnoId";
                            using (var cmd = new SQLiteCommand(queryTurno, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@total", venta.Total);
                                cmd.Parameters.AddWithValue("@turnoId", venta.TurnoId.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                        // 5. Registrar en auditoría
                        Helpers.AuditLogger.RegistrarCancelacionVenta(
                            usuarioActual,
                            ventaId,
                            venta.Total,
                            motivo,
                            new
                            {
                                venta.Id,
                                venta.Total,
                                venta.Fecha,
                                venta.UsuarioNombre,
                                ProductosCancelados = detalles.Count
                            }
                        );
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        #endregion

        #region Inventory Report Methods

        private void BtnActualizarInventario_Click(object sender, RoutedEventArgs e)
        {
            CargarReporteInventario();
        }

        private void CargarReporteInventario()
        {
            try
            {
                var productos = Producto.ObtenerTodos();
                dgInventario.ItemsSource = productos;

                // Calculate totals
                txtTotalProductos.Text = productos.Count.ToString();
                txtProductosBajoStock.Text = productos.Count(p => p.Stock < 10).ToString();
                txtValorInventario.Text = productos.Sum(p => p.Precio * p.Stock).ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar inventario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region Data Models

    public class Venta : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int? TurnoId { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; }
        public string Fecha { get; set; }
        public decimal Total { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal Cambio { get; set; }
        public string Estado { get; set; } = "Completada";
        public string MotivoCancelacion { get; set; }
        public string CanceladoPor { get; set; }
        public string FechaCancelacion { get; set; }

        public string TotalFormateado => Total.ToString("C");
        public string MontoPagadoFormateado => MontoPagado.ToString("C");
        public string CambioFormateado => Cambio.ToString("C");

        // Color según estado
        public string EstadoColor => Estado == "Cancelada" ? "#E74C3C" : "#27AE60";

        // Visibilidad del botón cancelar (solo visible si no está cancelada)
        public Visibility PuedeCancelar => Estado == "Completada" ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class DetalleVenta : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public string ProductoCodigo { get; set; }
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        public string PrecioUnitarioFormateado => PrecioUnitario.ToString("C");
        public string SubtotalFormateado => Subtotal.ToString("C");

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    #endregion
}
