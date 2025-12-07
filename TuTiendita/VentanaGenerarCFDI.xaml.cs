using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para generar una nueva factura CFDI desde una venta
    /// </summary>
    public partial class VentanaGenerarCFDI : Window
    {
        private ConfiguracionFiscal _configuracion;
        private Usuario _usuario;
        private List<dynamic> _ventasDisponibles;
        private dynamic _ventaSeleccionada;
        private List<dynamic> _detallesVenta;

        public VentanaGenerarCFDI(ConfiguracionFiscal configuracion, Usuario usuario)
        {
            InitializeComponent();
            _configuracion = configuracion;
            _usuario = usuario;

            CargarCatalogos();
            CargarVentasDisponibles();
        }

        private void CargarCatalogos()
        {
            // Cargar catálogos del SAT
            cmbReceptorRegimen.ItemsSource = CatalogosSAT.RegimenesFiscales;
            cmbUsoCFDI.ItemsSource = CatalogosSAT.UsosCFDI;
            cmbFormaPago.ItemsSource = CatalogosSAT.GetFormasPagoComunes();
            cmbMetodoPago.ItemsSource = CatalogosSAT.MetodosPago;

            // Valores por defecto
            cmbUsoCFDI.SelectedItem = CatalogosSAT.GetUsoCFDIPorClave("G03"); // Gastos en general
            cmbMetodoPago.SelectedItem = CatalogosSAT.GetMetodoPagoPorClave("PUE"); // Pago en una sola exhibición
            cmbFormaPago.SelectedItem = CatalogosSAT.GetFormaPagoPorClave("01"); // Efectivo
        }

        private void CargarVentasDisponibles()
        {
            try
            {
                _ventasDisponibles = FacturacionHelper.ObtenerVentasSinFacturar();
                lstVentas.ItemsSource = _ventasDisponibles;

                if (_ventasDisponibles.Count == 0)
                {
                    txtEstadoGeneracion.Text = "No hay ventas disponibles para facturar";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ventas: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscarVenta_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = txtBuscarVenta.Text?.Trim().ToLower();

            if (string.IsNullOrEmpty(busqueda))
            {
                lstVentas.ItemsSource = _ventasDisponibles;
            }
            else
            {
                var filtradas = _ventasDisponibles.Where(v =>
                    v.Id.ToString().Contains(busqueda) ||
                    (v.ClienteNombre?.ToLower().Contains(busqueda) ?? false)
                ).ToList();

                lstVentas.ItemsSource = filtradas;
            }
        }

        private void lstVentas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstVentas.SelectedItem != null)
            {
                _ventaSeleccionada = lstVentas.SelectedItem;
                CargarDetallesVenta((int)_ventaSeleccionada.Id);
                CargarDatosCliente();
                ActualizarResumen();
                btnGenerarCFDI.IsEnabled = true;
            }
            else
            {
                btnGenerarCFDI.IsEnabled = false;
            }
        }

        private void CargarDetallesVenta(int ventaId)
        {
            try
            {
                _detallesVenta = new List<dynamic>();

                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT * FROM DetalleVentas WHERE VentaId = @VentaId";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@VentaId", ventaId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _detallesVenta.Add(new
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    ProductoCodigo = reader["ProductoCodigo"]?.ToString(),
                                    ProductoNombre = reader["ProductoNombre"]?.ToString(),
                                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                                    PrecioUnitario = Convert.ToDecimal(reader["PrecioUnitario"]),
                                    Subtotal = Convert.ToDecimal(reader["Subtotal"])
                                });
                            }
                        }
                    }
                }

                dgConceptos.ItemsSource = _detallesVenta;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar detalles: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarDatosCliente()
        {
            // Si la venta tiene cliente asociado, cargar sus datos fiscales
            if (_ventaSeleccionada?.ClienteId != null)
            {
                try
                {
                    using (var conn = Database.GetConnection())
                    {
                        conn.Open();
                        string query = "SELECT * FROM Clientes WHERE Id = @Id";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", _ventaSeleccionada.ClienteId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string rfc = reader["RFC"]?.ToString();
                                    string razonSocial = reader["RazonSocial"]?.ToString();
                                    string regimenFiscal = reader["RegimenFiscalClave"]?.ToString();
                                    string cp = reader["DomicilioFiscalCP"]?.ToString();
                                    string usoCFDI = reader["UsoCFDIDefault"]?.ToString();

                                    if (!string.IsNullOrEmpty(rfc))
                                    {
                                        txtReceptorRFC.Text = rfc;
                                        txtReceptorNombre.Text = razonSocial ?? $"{reader["Nombre"]} {reader["Apellido"]}".Trim().ToUpper();
                                        txtReceptorCP.Text = cp ?? "";

                                        if (!string.IsNullOrEmpty(regimenFiscal))
                                        {
                                            cmbReceptorRegimen.SelectedItem = CatalogosSAT.GetRegimenFiscalPorClave(regimenFiscal);
                                        }

                                        if (!string.IsNullOrEmpty(usoCFDI))
                                        {
                                            cmbUsoCFDI.SelectedItem = CatalogosSAT.GetUsoCFDIPorClave(usoCFDI);
                                        }

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar cliente: {ex.Message}");
                }
            }

            // Si no hay datos, establecer público en general
            txtReceptorRFC.Text = CatalogosSAT.RFCPublicoGeneral;
            txtReceptorNombre.Text = "PUBLICO EN GENERAL";
            txtReceptorCP.Text = _configuracion?.CodigoPostal ?? "";
            cmbReceptorRegimen.SelectedItem = CatalogosSAT.GetRegimenFiscalPorClave("616"); // Sin obligaciones fiscales
            cmbUsoCFDI.SelectedItem = CatalogosSAT.GetUsoCFDIPorClave("S01"); // Sin efectos fiscales
        }

        private void ActualizarResumen()
        {
            if (_ventaSeleccionada == null) return;

            decimal subtotal = _detallesVenta?.Sum(d => (decimal)d.Subtotal) ?? 0;
            decimal iva = Math.Round(subtotal * 0.16m, 2);
            decimal total = subtotal + iva;

            txtSubtotal.Text = subtotal.ToString("C");
            txtIVA.Text = iva.ToString("C");
            txtDescuento.Text = "$0.00";
            txtTotal.Text = total.ToString("C");
        }

        private void btnCargarCliente_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar ventana para seleccionar cliente registrado
            var ventana = new VentanaSeleccionarClienteFiscal();
            if (ventana.ShowDialog() == true && ventana.ClienteSeleccionado != null)
            {
                var cliente = ventana.ClienteSeleccionado;
                txtReceptorRFC.Text = cliente.RFC ?? "";
                txtReceptorNombre.Text = cliente.RazonSocial ?? cliente.NombreCompleto.ToUpper();
                txtReceptorCP.Text = cliente.DomicilioFiscalCP ?? "";

                if (!string.IsNullOrEmpty(cliente.RegimenFiscalClave))
                {
                    cmbReceptorRegimen.SelectedItem = CatalogosSAT.GetRegimenFiscalPorClave(cliente.RegimenFiscalClave);
                }

                if (!string.IsNullOrEmpty(cliente.UsoCFDIDefault))
                {
                    cmbUsoCFDI.SelectedItem = CatalogosSAT.GetUsoCFDIPorClave(cliente.UsoCFDIDefault);
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnGenerarCFDI_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (_ventaSeleccionada == null)
            {
                MessageBox.Show("Debe seleccionar una venta para facturar.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtReceptorRFC.Text))
            {
                MessageBox.Show("El RFC del receptor es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReceptorRFC.Focus();
                return;
            }

            if (!CatalogosSAT.ValidarFormatoRFC(txtReceptorRFC.Text) &&
                txtReceptorRFC.Text != CatalogosSAT.RFCPublicoGeneral &&
                txtReceptorRFC.Text != CatalogosSAT.RFCExtranjero)
            {
                MessageBox.Show("El formato del RFC no es válido.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReceptorRFC.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtReceptorNombre.Text))
            {
                MessageBox.Show("El nombre del receptor es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReceptorNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtReceptorCP.Text) || txtReceptorCP.Text.Length != 5)
            {
                MessageBox.Show("El código postal debe tener 5 dígitos.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtReceptorCP.Focus();
                return;
            }

            if (cmbReceptorRegimen.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar el régimen fiscal del receptor.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbUsoCFDI.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar el uso del CFDI.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbFormaPago.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar la forma de pago.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbMetodoPago.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar el método de pago.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnGenerarCFDI.IsEnabled = false;
                txtEstadoGeneracion.Text = "Generando factura...";

                var regimen = cmbReceptorRegimen.SelectedItem as CatalogosSAT.RegimenFiscal;
                var usoCFDI = cmbUsoCFDI.SelectedItem as CatalogosSAT.UsoCFDI;
                var formaPago = cmbFormaPago.SelectedItem as CatalogosSAT.FormaPago;
                var metodoPago = cmbMetodoPago.SelectedItem as CatalogosSAT.MetodoPago;

                // Crear el CFDI
                var cfdi = FacturacionHelper.CrearCFDIDesdeVenta(
                    (int)_ventaSeleccionada.Id,
                    txtReceptorRFC.Text.Trim().ToUpper(),
                    txtReceptorNombre.Text.Trim().ToUpper(),
                    regimen.Clave,
                    txtReceptorCP.Text.Trim(),
                    usoCFDI.Clave,
                    formaPago.Clave,
                    metodoPago.Clave
                );

                cfdi.UsuarioId = _usuario.IdUsuario;
                cfdi.ClienteId = _ventaSeleccionada.ClienteId;

                // Guardar en base de datos
                int cfdiId = FacturacionHelper.GuardarCFDI(cfdi);

                txtEstadoGeneracion.Text = $"Factura generada con folio {cfdi.Serie}{cfdi.Folio}";

                MessageBox.Show(
                    $"Factura generada exitosamente.\n\n" +
                    $"Serie: {cfdi.Serie}\n" +
                    $"Folio: {cfdi.Folio}\n" +
                    $"Total: {cfdi.Total:C}\n\n" +
                    $"Estado: Pendiente de timbrado\n\n" +
                    $"Para timbrar la factura, configure un PAC y utilice la opción de timbrado.",
                    "Factura Generada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la factura: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnGenerarCFDI.IsEnabled = true;
                txtEstadoGeneracion.Text = "";
            }
        }
    }
}
