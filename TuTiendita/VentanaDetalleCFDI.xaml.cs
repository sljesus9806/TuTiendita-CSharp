using System;
using System.Windows;
using System.Windows.Media;
using TuTiendita.Helpers;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para mostrar el detalle de una factura CFDI
    /// </summary>
    public partial class VentanaDetalleCFDI : Window
    {
        private CFDI _cfdi;
        private ConfiguracionFiscal _configuracion;

        public VentanaDetalleCFDI(CFDI cfdi)
        {
            InitializeComponent();
            _cfdi = cfdi;
            _configuracion = FacturacionHelper.ObtenerConfiguracionFiscal();
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Encabezado
            txtFolio.Text = $"Factura {_cfdi.FolioCompleto}";
            txtFecha.Text = $"Fecha: {_cfdi.Fecha}";

            // Estado
            txtEstado.Text = _cfdi.Estado;
            brdEstado.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(_cfdi.EstadoColor));

            // Datos de timbrado
            if (_cfdi.EstaTimbrado)
            {
                pnlTimbrado.Visibility = Visibility.Visible;
                txtUUID.Text = _cfdi.UUID;
                txtFechaTimbrado.Text = _cfdi.FechaTimbrado;
                btnTimbrar.Visibility = Visibility.Collapsed;
            }
            else
            {
                pnlTimbrado.Visibility = Visibility.Collapsed;
                btnTimbrar.Visibility = Visibility.Visible;
            }

            // Botón de cancelación
            btnCancelarCFDI.Visibility = _cfdi.PuedeCancelarse ? Visibility.Visible : Visibility.Collapsed;

            // Emisor
            txtEmisorRFC.Text = _cfdi.EmisorRFC;
            txtEmisorNombre.Text = _cfdi.EmisorNombre;
            txtEmisorRegimen.Text = $"{_cfdi.EmisorRegimenFiscal} - {CatalogosSAT.GetRegimenFiscalPorClave(_cfdi.EmisorRegimenFiscal)?.Descripcion ?? ""}";

            // Receptor
            txtReceptorRFC.Text = _cfdi.ReceptorRFC;
            txtReceptorNombre.Text = _cfdi.ReceptorNombre;
            txtUsoCFDI.Text = $"{_cfdi.ReceptorUsoCFDI} - {_cfdi.UsoCFDIDescripcion}";
            txtReceptorCP.Text = _cfdi.ReceptorDomicilioFiscalCP;

            // Datos del comprobante
            txtTipoComprobante.Text = _cfdi.TipoComprobanteDescripcion;
            txtFormaPago.Text = $"{_cfdi.FormaPagoClave} - {_cfdi.FormaPagoDescripcion}";
            txtMetodoPago.Text = $"{_cfdi.MetodoPagoClave} - {_cfdi.MetodoPagoDescripcion}";
            txtMoneda.Text = _cfdi.Moneda;

            // Conceptos
            dgConceptos.ItemsSource = _cfdi.Conceptos;

            // Totales
            txtSubtotal.Text = _cfdi.Subtotal.ToString("C");
            txtIVA.Text = _cfdi.IVATrasladado.ToString("C");

            if (_cfdi.Descuento > 0)
            {
                rowDescuento.Visibility = Visibility.Visible;
                txtDescuento.Text = $"-{_cfdi.Descuento:C}";
            }

            txtTotal.Text = _cfdi.Total.ToString("C");
        }

        private void btnTimbrar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "El timbrado de facturas requiere la configuración de un PAC (Proveedor Autorizado de Certificación).\n\n" +
                "Opciones disponibles:\n" +
                "1. Configure un PAC en la Configuración Fiscal\n" +
                "2. Exporte el XML y tímbrelo manualmente en el portal del SAT\n" +
                "3. Utilice un servicio de timbrado externo\n\n" +
                "Una vez timbrado, puede importar el XML con el timbre fiscal.",
                "Timbrado de Factura",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnCancelarCFDI_Click(object sender, RoutedEventArgs e)
        {
            if (!_cfdi.PuedeCancelarse)
            {
                MessageBox.Show("Esta factura no puede ser cancelada.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de cancelar la factura {_cfdi.FolioCompleto}?\n\n" +
                "UUID: {_cfdi.UUID}\n\n" +
                "Esta acción requerirá autorización del SAT y puede tardar hasta 72 horas.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show(
                    "La cancelación de facturas timbradas requiere conexión con el SAT.\n\n" +
                    "Configure un PAC para realizar cancelaciones automáticas o " +
                    "cancele manualmente en el portal del SAT.",
                    "Cancelación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void btnDescargarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"CFDI_{_cfdi.Serie}{_cfdi.Folio}_{_cfdi.ReceptorRFC}",
                    DefaultExt = ".pdf",
                    Filter = "Archivos PDF|*.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    CFDIPdfGenerator.GenerarPDF(_cfdi, _configuracion, saveDialog.FileName);
                    MessageBox.Show("PDF generado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar PDF: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDescargarXML_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string xml = !string.IsNullOrEmpty(_cfdi.XMLTimbrado) ? _cfdi.XMLTimbrado : _cfdi.XMLOriginal;

                if (string.IsNullOrEmpty(xml))
                {
                    MessageBox.Show("No hay XML disponible para esta factura.", "Información",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"CFDI_{_cfdi.Serie}{_cfdi.Folio}_{_cfdi.ReceptorRFC}",
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar XML: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
