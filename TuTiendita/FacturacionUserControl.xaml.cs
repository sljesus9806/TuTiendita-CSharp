using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TuTiendita.Helpers;

namespace TuTiendita
{
    /// <summary>
    /// Módulo de Facturación Electrónica CFDI 4.0 para México
    /// </summary>
    public partial class FacturacionUserControl : UserControl
    {
        private Usuario _usuarioActual;
        private List<CFDI> _facturas;
        private ConfiguracionFiscal _configuracionFiscal;

        public FacturacionUserControl(Usuario usuario)
        {
            InitializeComponent();
            _usuarioActual = usuario;

            // Establecer fechas por defecto (mes actual)
            dpFechaInicio.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpFechaFin.SelectedDate = DateTime.Now;

            CargarDatos();
        }

        private void CargarDatos()
        {
            CargarConfiguracionFiscal();
            CargarEstadisticas();
            CargarFacturas();
        }

        private void CargarConfiguracionFiscal()
        {
            try
            {
                _configuracionFiscal = FacturacionHelper.ObtenerConfiguracionFiscal();

                if (_configuracionFiscal != null && _configuracionFiscal.ConfiguracionCompleta)
                {
                    txtConfigStatus.Text = $"Emisor: {_configuracionFiscal.RazonSocial} | RFC: {_configuracionFiscal.RFC}";
                    txtConfigStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
                    btnNuevaFactura.IsEnabled = true;
                }
                else
                {
                    txtConfigStatus.Text = "Configure los datos fiscales del emisor para poder facturar";
                    txtConfigStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF9800"));
                    btnNuevaFactura.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración fiscal: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarEstadisticas()
        {
            try
            {
                var stats = FacturacionHelper.ObtenerEstadisticasCFDI(
                    dpFechaInicio.SelectedDate,
                    dpFechaFin.SelectedDate);

                txtTotalFacturas.Text = stats.Total.ToString();
                txtTimbradas.Text = stats.Timbrados.ToString();
                txtPendientes.Text = stats.Pendientes.ToString();
                txtCanceladas.Text = stats.Cancelados.ToString();
                txtMontoTotal.Text = stats.MontoTotal.ToString("C");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar estadísticas: {ex.Message}");
            }
        }

        private void CargarFacturas()
        {
            try
            {
                string filtroEstado = null;
                if (cmbFiltroEstado.SelectedIndex > 0)
                {
                    filtroEstado = ((ComboBoxItem)cmbFiltroEstado.SelectedItem).Content.ToString();
                }

                string busqueda = txtBuscar.Text?.Trim();

                if (!string.IsNullOrEmpty(busqueda))
                {
                    _facturas = FacturacionHelper.BuscarCFDI(busqueda);
                }
                else
                {
                    _facturas = FacturacionHelper.ObtenerTodosCFDI(
                        filtroEstado,
                        dpFechaInicio.SelectedDate,
                        dpFechaFin.SelectedDate);
                }

                dgFacturas.ItemsSource = _facturas;

                // Mostrar mensaje si no hay facturas
                panelSinFacturas.Visibility = _facturas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                dgFacturas.Visibility = _facturas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar facturas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnConfigurarEmisor_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VentanaConfiguracionFiscal(_configuracionFiscal);
            if (ventana.ShowDialog() == true)
            {
                CargarConfiguracionFiscal();
                MessageBox.Show("Configuración fiscal guardada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnNuevaFactura_Click(object sender, RoutedEventArgs e)
        {
            if (_configuracionFiscal == null || !_configuracionFiscal.ConfiguracionCompleta)
            {
                MessageBox.Show("Debe configurar los datos fiscales del emisor antes de crear facturas.",
                    "Configuración requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ventana = new VentanaGenerarCFDI(_configuracionFiscal, _usuarioActual);
            if (ventana.ShowDialog() == true)
            {
                CargarDatos();
                MessageBox.Show("Factura creada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void cmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                CargarEstadisticas();
                CargarFacturas();
            }
        }

        private void dpFecha_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                CargarEstadisticas();
                CargarFacturas();
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                CargarFacturas();
            }
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbFiltroEstado.SelectedIndex = 0;
            dpFechaInicio.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpFechaFin.SelectedDate = DateTime.Now;
            txtBuscar.Text = "";
            CargarDatos();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarDatos();
        }

        private void dgFacturas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgFacturas.SelectedItem is CFDI cfdi)
            {
                MostrarDetalleFactura(cfdi.Id);
            }
        }

        private void btnVerFactura_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int id))
            {
                MostrarDetalleFactura(id);
            }
        }

        private void MostrarDetalleFactura(int cfdiId)
        {
            try
            {
                var cfdi = FacturacionHelper.ObtenerCFDIPorId(cfdiId);
                if (cfdi != null)
                {
                    var ventana = new VentanaDetalleCFDI(cfdi);
                    ventana.ShowDialog();
                    CargarDatos(); // Recargar por si hubo cambios
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar factura: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDescargarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int id))
            {
                try
                {
                    var cfdi = FacturacionHelper.ObtenerCFDIPorId(id);
                    if (cfdi != null)
                    {
                        var saveDialog = new Microsoft.Win32.SaveFileDialog
                        {
                            FileName = $"CFDI_{cfdi.Serie}{cfdi.Folio}_{cfdi.ReceptorRFC}",
                            DefaultExt = ".pdf",
                            Filter = "Archivos PDF|*.pdf"
                        };

                        if (saveDialog.ShowDialog() == true)
                        {
                            CFDIPdfGenerator.GenerarPDF(cfdi, _configuracionFiscal, saveDialog.FileName);
                            MessageBox.Show("PDF generado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // Abrir el archivo
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnDescargarXML_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int id))
            {
                try
                {
                    var cfdi = FacturacionHelper.ObtenerCFDIPorId(id);
                    if (cfdi != null)
                    {
                        string xml = !string.IsNullOrEmpty(cfdi.XMLTimbrado) ? cfdi.XMLTimbrado : cfdi.XMLOriginal;

                        if (string.IsNullOrEmpty(xml))
                        {
                            MessageBox.Show("No hay XML disponible para esta factura.", "Información",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var saveDialog = new Microsoft.Win32.SaveFileDialog
                        {
                            FileName = $"CFDI_{cfdi.Serie}{cfdi.Folio}_{cfdi.ReceptorRFC}",
                            DefaultExt = ".xml",
                            Filter = "Archivos XML|*.xml"
                        };

                        if (saveDialog.ShowDialog() == true)
                        {
                            System.IO.File.WriteAllText(saveDialog.FileName, xml);
                            MessageBox.Show("XML guardado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar XML: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
