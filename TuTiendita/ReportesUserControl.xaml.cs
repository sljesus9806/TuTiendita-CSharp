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
        public ReportesUserControl()
        {
            InitializeComponent();
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
                    // Preguntar si desea abrir el reporte
                    var resultado = MessageBox.Show(
                        "✓ Reporte de ventas exportado exitosamente a PDF\n\n¿Desea abrir el reporte?",
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
                int totalProductos = 0;

                string fechaDesde = dpFechaDesde.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00");
                string fechaHasta = dpFechaHasta.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59");

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Get sales
                    string query = @"SELECT * FROM Ventas
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
                                ventas.Add(new Venta
                                {
                                    Id = reader.GetInt32(0),
                                    TurnoId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                    UsuarioId = reader.GetInt32(2),
                                    UsuarioNombre = reader.GetString(3),
                                    Fecha = reader.GetString(4),
                                    Total = reader.GetDecimal(5),
                                    MontoPagado = reader.GetDecimal(6),
                                    Cambio = reader.GetDecimal(7)
                                });

                                totalVentas += reader.GetDecimal(5);
                            }
                        }
                    }

                    // Get total products sold
                    string productsQuery = @"SELECT SUM(dv.Cantidad)
                                           FROM DetalleVentas dv
                                           INNER JOIN Ventas v ON dv.VentaId = v.Id
                                           WHERE v.Fecha BETWEEN @fechaDesde AND @fechaHasta";

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
                txtTotalVentas.Text = totalVentas.ToString("C");
                txtNumTransacciones.Text = ventas.Count.ToString();
                txtVentaPromedio.Text = ventas.Count > 0 ? (totalVentas / ventas.Count).ToString("C") : "$0.00";
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

                dgDetalleVenta.ItemsSource = detalles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalle de venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        public string TotalFormateado => Total.ToString("C");
        public string MontoPagadoFormateado => MontoPagado.ToString("C");
        public string CambioFormateado => Cambio.ToString("C");

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
