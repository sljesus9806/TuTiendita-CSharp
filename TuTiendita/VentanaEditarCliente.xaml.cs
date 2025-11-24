using System;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaEditarCliente : Window
    {
        private Cliente clienteActual;
        private Usuario usuarioActual;
        private bool esNuevo;

        public VentanaEditarCliente(Cliente cliente, Usuario usuario)
        {
            InitializeComponent();
            clienteActual = cliente;
            usuarioActual = usuario;
            esNuevo = (cliente == null);

            if (esNuevo)
            {
                txtTitulo.Text = "➕ Nuevo Cliente";
                clienteActual = new Cliente
                {
                    FechaRegistro = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Activo = true
                };
            }
            else
            {
                txtTitulo.Text = "✏️ Editar Cliente";
                CargarDatosCliente();
            }
        }

        private void CargarDatosCliente()
        {
            if (clienteActual == null) return;

            txtNombre.Text = clienteActual.Nombre;
            txtApellido.Text = clienteActual.Apellido;
            txtDocumento.Text = clienteActual.Documento;
            txtTelefono.Text = clienteActual.Telefono;
            txtEmail.Text = clienteActual.Email;
            txtDireccion.Text = clienteActual.Direccion;
            txtLimiteCredito.Text = clienteActual.LimiteCredito.ToString();
            txtDescuentoEspecial.Text = clienteActual.DescuentoEspecial.ToString();
            txtNotas.Text = clienteActual.Notas;

            if (!string.IsNullOrWhiteSpace(clienteActual.FechaNacimiento))
            {
                if (DateTime.TryParse(clienteActual.FechaNacimiento, out DateTime fecha))
                {
                    dpFechaNacimiento.SelectedDate = fecha;
                }
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            // Validar límite de crédito
            if (!double.TryParse(txtLimiteCredito.Text, out double limiteCredito) || limiteCredito < 0)
            {
                MessageBox.Show("El límite de crédito debe ser un número válido mayor o igual a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLimiteCredito.Focus();
                return;
            }

            // Validar descuento especial
            if (!double.TryParse(txtDescuentoEspecial.Text, out double descuentoEspecial) || descuentoEspecial < 0 || descuentoEspecial > 100)
            {
                MessageBox.Show("El descuento especial debe ser un número entre 0 y 100.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescuentoEspecial.Focus();
                return;
            }

            try
            {
                // Actualizar datos del cliente
                clienteActual.Nombre = txtNombre.Text.Trim();
                clienteActual.Apellido = txtApellido.Text.Trim();
                clienteActual.Documento = txtDocumento.Text.Trim();
                clienteActual.Telefono = txtTelefono.Text.Trim();
                clienteActual.Email = txtEmail.Text.Trim();
                clienteActual.Direccion = txtDireccion.Text.Trim();
                clienteActual.LimiteCredito = limiteCredito;
                clienteActual.DescuentoEspecial = descuentoEspecial;
                clienteActual.Notas = txtNotas.Text.Trim();

                if (dpFechaNacimiento.SelectedDate.HasValue)
                {
                    clienteActual.FechaNacimiento = dpFechaNacimiento.SelectedDate.Value.ToString("yyyy-MM-dd");
                }

                bool exito;
                if (esNuevo)
                {
                    exito = ClientesHelper.CrearCliente(clienteActual, usuarioActual);
                }
                else
                {
                    exito = ClientesHelper.ActualizarCliente(clienteActual, usuarioActual);
                }

                if (exito)
                {
                    MessageBox.Show(
                        esNuevo ? "Cliente creado correctamente." : "Cliente actualizado correctamente.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error al guardar el cliente.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el cliente: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
