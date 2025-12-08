using System;
using System.Windows;
using Microsoft.Win32;
using TuTiendita.Helpers;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para configurar los datos fiscales del emisor
    /// </summary>
    public partial class VentanaConfiguracionFiscal : Window
    {
        private ConfiguracionFiscal _configuracion;

        public VentanaConfiguracionFiscal(ConfiguracionFiscal configuracionExistente = null)
        {
            InitializeComponent();
            _configuracion = configuracionExistente ?? new ConfiguracionFiscal();
            CargarRegimenesFiscales();
            CargarDatosExistentes();
        }

        private void CargarRegimenesFiscales()
        {
            cmbRegimenFiscal.ItemsSource = CatalogosSAT.RegimenesFiscales;
        }

        private void CargarDatosExistentes()
        {
            if (_configuracion != null)
            {
                txtRFC.Text = _configuracion.RFC ?? "";
                txtRazonSocial.Text = _configuracion.RazonSocial ?? "";
                txtCalle.Text = _configuracion.Calle ?? "";
                txtNumExterior.Text = _configuracion.NumeroExterior ?? "";
                txtNumInterior.Text = _configuracion.NumeroInterior ?? "";
                txtColonia.Text = _configuracion.Colonia ?? "";
                txtCodigoPostal.Text = _configuracion.CodigoPostal ?? "";
                txtMunicipio.Text = _configuracion.Municipio ?? "";
                txtEstado.Text = _configuracion.Estado ?? "";
                txtSerie.Text = _configuracion.SerieFactura ?? "A";
                txtUltimoFolio.Text = _configuracion.UltimoFolio.ToString();
                txtLugarExpedicion.Text = _configuracion.LugarExpedicion ?? "";
                txtCertificado.Text = _configuracion.CertificadoCSD ?? "";
                txtLlave.Text = _configuracion.LlaveCSD ?? "";
                txtPACUsuario.Text = _configuracion.PACUsuario ?? "";
                chkModoProduccion.IsChecked = _configuracion.PACModoProduccion;

                // Seleccionar régimen fiscal
                if (!string.IsNullOrEmpty(_configuracion.RegimenFiscalClave))
                {
                    var regimen = CatalogosSAT.GetRegimenFiscalPorClave(_configuracion.RegimenFiscalClave);
                    if (regimen != null)
                    {
                        cmbRegimenFiscal.SelectedItem = regimen;
                    }
                }

                // Seleccionar PAC
                if (!string.IsNullOrEmpty(_configuracion.PAC))
                {
                    for (int i = 0; i < cmbPAC.Items.Count; i++)
                    {
                        if (cmbPAC.Items[i].ToString().Contains(_configuracion.PAC))
                        {
                            cmbPAC.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void btnSeleccionarCer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar Certificado CSD",
                Filter = "Archivos de certificado|*.cer|Todos los archivos|*.*",
                DefaultExt = ".cer"
            };

            if (dialog.ShowDialog() == true)
            {
                txtCertificado.Text = dialog.FileName;
            }
        }

        private void btnSeleccionarKey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar Llave Privada CSD",
                Filter = "Archivos de llave|*.key|Todos los archivos|*.*",
                DefaultExt = ".key"
            };

            if (dialog.ShowDialog() == true)
            {
                txtLlave.Text = dialog.FileName;
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(txtRFC.Text))
            {
                MessageBox.Show("El RFC es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRFC.Focus();
                return;
            }

            if (!CatalogosSAT.ValidarFormatoRFC(txtRFC.Text))
            {
                MessageBox.Show("El formato del RFC no es válido.\n\n" +
                    "- Persona Física: 13 caracteres (AAAA000000XXX)\n" +
                    "- Persona Moral: 12 caracteres (AAA000000XXX)",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRFC.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRazonSocial.Text))
            {
                MessageBox.Show("La razón social es obligatoria.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRazonSocial.Focus();
                return;
            }

            if (cmbRegimenFiscal.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un régimen fiscal.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbRegimenFiscal.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCodigoPostal.Text) || txtCodigoPostal.Text.Length != 5)
            {
                MessageBox.Show("El código postal debe tener 5 dígitos.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCodigoPostal.Focus();
                return;
            }

            try
            {
                var regimen = cmbRegimenFiscal.SelectedItem as CatalogosSAT.RegimenFiscal;

                // Validar que el régimen sea válido para el tipo de persona
                bool esPersonaFisica = CatalogosSAT.EsPersonaFisica(txtRFC.Text);
                if (esPersonaFisica && !regimen.PersonaFisica)
                {
                    MessageBox.Show("El régimen fiscal seleccionado no aplica para personas físicas.",
                        "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!esPersonaFisica && !regimen.PersonaMoral)
                {
                    MessageBox.Show("El régimen fiscal seleccionado no aplica para personas morales.",
                        "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear objeto de configuración
                _configuracion.RFC = txtRFC.Text.Trim().ToUpper();
                _configuracion.RazonSocial = txtRazonSocial.Text.Trim().ToUpper();
                _configuracion.RegimenFiscalClave = regimen.Clave;
                _configuracion.CodigoPostal = txtCodigoPostal.Text.Trim();
                _configuracion.Calle = txtCalle.Text.Trim();
                _configuracion.NumeroExterior = txtNumExterior.Text.Trim();
                _configuracion.NumeroInterior = txtNumInterior.Text.Trim();
                _configuracion.Colonia = txtColonia.Text.Trim();
                _configuracion.Municipio = txtMunicipio.Text.Trim();
                _configuracion.Estado = txtEstado.Text.Trim();
                _configuracion.SerieFactura = string.IsNullOrWhiteSpace(txtSerie.Text) ? "A" : txtSerie.Text.Trim().ToUpper();

                if (int.TryParse(txtUltimoFolio.Text, out int ultimoFolio))
                {
                    _configuracion.UltimoFolio = ultimoFolio;
                }

                _configuracion.LugarExpedicion = string.IsNullOrWhiteSpace(txtLugarExpedicion.Text)
                    ? txtCodigoPostal.Text.Trim()
                    : txtLugarExpedicion.Text.Trim();

                // Datos de certificados y PAC
                _configuracion.CertificadoCSD = txtCertificado.Text;
                _configuracion.LlaveCSD = txtLlave.Text;
                _configuracion.ContrasenaLlaveCSD = txtContrasenaLlave.Password;

                if (cmbPAC.SelectedIndex > 0)
                {
                    _configuracion.PAC = (cmbPAC.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                }
                _configuracion.PACUsuario = txtPACUsuario.Text;
                _configuracion.PACContrasena = txtPACContrasena.Password;
                _configuracion.PACModoProduccion = chkModoProduccion.IsChecked ?? false;

                // Guardar
                FacturacionHelper.GuardarConfiguracionFiscal(_configuracion);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
