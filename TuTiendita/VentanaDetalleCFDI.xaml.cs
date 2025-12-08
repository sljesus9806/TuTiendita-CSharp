using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using TuTiendita.Helpers;
using TuTiendita.Services;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para mostrar el detalle de una factura CFDI
    /// </summary>
    public partial class VentanaDetalleCFDI : Window
    {
        private CFDI _cfdi;
        private ConfiguracionFiscal _configuracion;
        private bool _isTimbrandoOCancelando = false;

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

        private async void btnTimbrar_Click(object sender, RoutedEventArgs e)
        {
            // Validar que no esté en proceso
            if (_isTimbrandoOCancelando)
            {
                MessageBox.Show("Ya hay una operación en proceso. Por favor espere.",
                    "Operación en Proceso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar configuración del PAC
            if (!ValidarConfiguracionPAC())
            {
                return;
            }

            // Confirmar timbrado
            var confirmResult = MessageBox.Show(
                $"¿Desea timbrar la factura {_cfdi.FolioCompleto}?\n\n" +
                $"Receptor: {_cfdi.ReceptorNombre}\n" +
                $"RFC: {_cfdi.ReceptorRFC}\n" +
                $"Total: {_cfdi.Total:C}\n\n" +
                (_configuracion.PACModoProduccion
                    ? "⚠️ MODO PRODUCCIÓN - Se consumirá un timbre real"
                    : "ℹ️ Modo Sandbox - Timbrado de prueba"),
                "Confirmar Timbrado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes) return;

            _isTimbrandoOCancelando = true;
            btnTimbrar.IsEnabled = false;
            btnTimbrar.Content = "Timbrando...";

            try
            {
                // Obtener XML a timbrar
                string xmlATimbrar = _cfdi.XMLOriginal;

                if (string.IsNullOrEmpty(xmlATimbrar))
                {
                    MessageBox.Show("No hay XML generado para esta factura. Por favor regenere la factura.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Firmar el XML si tenemos certificados
                if (!string.IsNullOrEmpty(_configuracion.CertificadoCSD) &&
                    !string.IsNullOrEmpty(_configuracion.LlaveCSD) &&
                    File.Exists(_configuracion.CertificadoCSD) &&
                    File.Exists(_configuracion.LlaveCSD))
                {
                    try
                    {
                        xmlATimbrar = FinkokService.FirmarXML(
                            xmlATimbrar,
                            _configuracion.CertificadoCSD,
                            _configuracion.LlaveCSD,
                            _configuracion.ContrasenaLlaveCSD ?? "");
                    }
                    catch (Exception exFirma)
                    {
                        MessageBox.Show($"Error al firmar el XML: {exFirma.Message}\n\n" +
                            "Verifique que los certificados CSD sean válidos y la contraseña sea correcta.",
                            "Error de Firma", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Crear servicio Finkok
                var finkok = new FinkokService(
                    _configuracion.PACUsuario,
                    _configuracion.PACContrasena,
                    _configuracion.PACModoProduccion);

                // Timbrar
                var resultado = await finkok.TimbrarAsync(xmlATimbrar);

                if (resultado.Success)
                {
                    // Actualizar CFDI con datos del timbre
                    _cfdi.UUID = resultado.UUID;
                    _cfdi.FechaTimbrado = resultado.FechaTimbrado;
                    _cfdi.XMLTimbrado = resultado.XMLTimbrado;
                    _cfdi.SelloDigitalCFDI = resultado.SelloCFDI;
                    _cfdi.SelloSAT = resultado.SelloSAT;
                    _cfdi.NoCertificadoEmisor = resultado.NoCertificadoCFDI;
                    _cfdi.NoCertificadoSAT = resultado.NoCertificadoSAT;
                    _cfdi.CadenaOriginal = resultado.CadenaOriginalTFD;
                    _cfdi.Estado = "Timbrado";

                    // Guardar en base de datos
                    bool guardado = FacturacionHelper.ActualizarCFDITimbrado(_cfdi);

                    if (guardado)
                    {
                        MessageBox.Show(
                            $"¡Factura timbrada exitosamente!\n\n" +
                            $"UUID: {resultado.UUID}\n" +
                            $"Fecha de Timbrado: {resultado.FechaTimbrado}",
                            "Timbrado Exitoso",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Actualizar UI
                        CargarDatos();
                    }
                    else
                    {
                        MessageBox.Show(
                            "El timbrado fue exitoso pero hubo un error al guardar en la base de datos.\n\n" +
                            $"UUID: {resultado.UUID}\n\n" +
                            "Guarde el XML manualmente.",
                            "Advertencia",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    string errorMsg = $"Error al timbrar:\n\n" +
                        $"Código: {resultado.ErrorCode}\n" +
                        $"Mensaje: {resultado.ErrorMessage}";

                    // Errores comunes de Finkok
                    if (resultado.ErrorCode == "301")
                        errorMsg += "\n\nEl XML ya fue timbrado previamente.";
                    else if (resultado.ErrorCode == "401")
                        errorMsg += "\n\nCredenciales de Finkok inválidas.";
                    else if (resultado.ErrorCode == "CFDI33101")
                        errorMsg += "\n\nEl certificado no corresponde al emisor.";

                    MessageBox.Show(errorMsg, "Error de Timbrado",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado al timbrar:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isTimbrandoOCancelando = false;
                btnTimbrar.IsEnabled = true;
                btnTimbrar.Content = "Timbrar Factura";
            }
        }

        private bool ValidarConfiguracionPAC()
        {
            if (_configuracion == null)
            {
                MessageBox.Show("No hay configuración fiscal. Configure los datos fiscales primero.",
                    "Configuración Requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_configuracion.PAC) || _configuracion.PAC == "(Sin configurar)")
            {
                MessageBox.Show(
                    "No hay un PAC configurado.\n\n" +
                    "Vaya a Configuración Fiscal y configure un PAC (Finkok recomendado) " +
                    "con sus credenciales para poder timbrar.",
                    "PAC No Configurado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (_configuracion.PAC != "Finkok")
            {
                MessageBox.Show(
                    $"El PAC '{_configuracion.PAC}' no está soportado actualmente.\n\n" +
                    "Por favor configure Finkok como PAC.",
                    "PAC No Soportado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrEmpty(_configuracion.PACUsuario) ||
                string.IsNullOrEmpty(_configuracion.PACContrasena))
            {
                MessageBox.Show(
                    "Faltan las credenciales del PAC (usuario y/o contraseña).\n\n" +
                    "Configure las credenciales de Finkok en la Configuración Fiscal.",
                    "Credenciales Requeridas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async void btnCancelarCFDI_Click(object sender, RoutedEventArgs e)
        {
            if (!_cfdi.PuedeCancelarse)
            {
                MessageBox.Show("Esta factura no puede ser cancelada.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isTimbrandoOCancelando)
            {
                MessageBox.Show("Ya hay una operación en proceso. Por favor espere.",
                    "Operación en Proceso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar configuración del PAC
            if (!ValidarConfiguracionPAC())
            {
                return;
            }

            // Validar que tenemos certificados para cancelar
            if (string.IsNullOrEmpty(_configuracion.CertificadoCSD) ||
                string.IsNullOrEmpty(_configuracion.LlaveCSD) ||
                !File.Exists(_configuracion.CertificadoCSD) ||
                !File.Exists(_configuracion.LlaveCSD))
            {
                MessageBox.Show(
                    "Se requieren los certificados CSD para cancelar facturas.\n\n" +
                    "Configure los certificados en la Configuración Fiscal.",
                    "Certificados Requeridos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Solicitar motivo de cancelación
            var motivoWindow = new VentanaMotivoCancelacion();
            if (motivoWindow.ShowDialog() != true)
            {
                return;
            }

            string motivo = motivoWindow.MotivoCancelacion;
            string folioSustitucion = motivoWindow.FolioSustitucion;

            // Confirmar cancelación
            var result = MessageBox.Show(
                $"¿Está seguro de cancelar la factura {_cfdi.FolioCompleto}?\n\n" +
                $"UUID: {_cfdi.UUID}\n" +
                $"Motivo: {ObtenerDescripcionMotivo(motivo)}\n\n" +
                "⚠️ Esta acción puede tardar en procesarse y requiere aceptación del receptor.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _isTimbrandoOCancelando = true;
            btnCancelarCFDI.IsEnabled = false;

            try
            {
                // Leer certificados
                byte[] certBytes = File.ReadAllBytes(_configuracion.CertificadoCSD);
                byte[] keyBytes = File.ReadAllBytes(_configuracion.LlaveCSD);
                string certBase64 = Convert.ToBase64String(certBytes);
                string keyBase64 = Convert.ToBase64String(keyBytes);

                // Crear servicio Finkok
                var finkok = new FinkokService(
                    _configuracion.PACUsuario,
                    _configuracion.PACContrasena,
                    _configuracion.PACModoProduccion);

                // Cancelar
                var resultado = await finkok.CancelarAsync(
                    _cfdi.EmisorRFC,
                    _cfdi.UUID,
                    certBase64,
                    keyBase64,
                    _configuracion.ContrasenaLlaveCSD ?? "",
                    motivo,
                    folioSustitucion);

                if (resultado.Success)
                {
                    // Actualizar estado
                    string nuevoEstado = resultado.EstatusUUID == "202" ? "Cancelacion en Proceso" : "Cancelado";
                    _cfdi.Estado = nuevoEstado;
                    _cfdi.FechaCancelacion = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    FacturacionHelper.ActualizarEstadoCFDI(_cfdi.Id, nuevoEstado, _cfdi.FechaCancelacion);

                    string mensaje = resultado.EstatusUUID == "202"
                        ? "La solicitud de cancelación ha sido enviada.\n\n" +
                          "El receptor tiene hasta 72 horas para aceptar o rechazar la cancelación."
                        : "La factura ha sido cancelada exitosamente.";

                    MessageBox.Show(mensaje, "Cancelación",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    CargarDatos();
                }
                else
                {
                    MessageBox.Show(
                        $"Error al cancelar:\n\n" +
                        $"Código: {resultado.ErrorCode}\n" +
                        $"Mensaje: {resultado.ErrorMessage}",
                        "Error de Cancelación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inesperado al cancelar:\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isTimbrandoOCancelando = false;
                btnCancelarCFDI.IsEnabled = true;
            }
        }

        private string ObtenerDescripcionMotivo(string motivo)
        {
            return motivo switch
            {
                "01" => "01 - Comprobante emitido con errores con relación",
                "02" => "02 - Comprobante emitido con errores sin relación",
                "03" => "03 - No se llevó a cabo la operación",
                "04" => "04 - Operación nominativa relacionada en una factura global",
                _ => motivo
            };
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
