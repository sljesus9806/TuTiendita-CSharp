using System;
using System.Text.RegularExpressions;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaEditarCliente : Window
    {
        private Cliente clienteActual;
        private Usuario usuarioActual;
        private bool esNuevo;
        private bool hayCambios = false;

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

            // Registrar cambios en los campos
            txtNombre.TextChanged += (s, e) => hayCambios = true;
            txtApellido.TextChanged += (s, e) => hayCambios = true;
            txtDocumento.TextChanged += (s, e) => hayCambios = true;
            txtTelefono.TextChanged += (s, e) => hayCambios = true;
            txtEmail.TextChanged += (s, e) => hayCambios = true;
            txtDireccion.TextChanged += (s, e) => hayCambios = true;
            txtLimiteCredito.TextChanged += (s, e) => hayCambios = true;
            txtDescuentoEspecial.TextChanged += (s, e) => hayCambios = true;
            txtNotas.TextChanged += (s, e) => hayCambios = true;
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

        private bool ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email no es requerido

            // Patrón básico de email
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            // Validar formato de email
            string email = txtEmail.Text.Trim();
            if (!ValidarEmail(email))
            {
                MessageBox.Show("El formato del correo electronico no es valido.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return;
            }

            // Validar documento duplicado
            string documento = txtDocumento.Text.Trim();
            if (!string.IsNullOrWhiteSpace(documento))
            {
                var clienteExistente = ClientesHelper.BuscarPorDocumento(documento);
                if (clienteExistente != null && (esNuevo || clienteExistente.Id != clienteActual.Id))
                {
                    MessageBox.Show($"Ya existe un cliente con el documento '{documento}'.\n" +
                        $"Cliente: {clienteExistente.NombreCompleto}", "Documento Duplicado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDocumento.Focus();
                    return;
                }
            }

            // Validar límite de crédito
            if (!double.TryParse(txtLimiteCredito.Text, out double limiteCredito) || limiteCredito < 0)
            {
                MessageBox.Show("El limite de credito debe ser un numero valido mayor o igual a 0.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLimiteCredito.Focus();
                return;
            }

            // Advertencia si el límite de crédito es muy alto
            if (limiteCredito > 50000)
            {
                var resultado = MessageBox.Show(
                    $"El limite de credito ({limiteCredito:C}) es muy alto.\n" +
                    "¿Esta seguro de continuar?",
                    "Advertencia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    txtLimiteCredito.Focus();
                    return;
                }
            }

            // Validar descuento especial
            if (!double.TryParse(txtDescuentoEspecial.Text, out double descuentoEspecial) || descuentoEspecial < 0 || descuentoEspecial > 100)
            {
                MessageBox.Show("El descuento especial debe ser un numero entre 0 y 100.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescuentoEspecial.Focus();
                return;
            }

            // Advertencia si el descuento es muy alto
            if (descuentoEspecial > 30)
            {
                var resultado = MessageBox.Show(
                    $"El descuento especial ({descuentoEspecial}%) es muy alto.\n" +
                    "¿Esta seguro de continuar?",
                    "Advertencia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    txtDescuentoEspecial.Focus();
                    return;
                }
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
                    int nuevoId = ClientesHelper.CrearCliente(clienteActual, usuarioActual);
                    exito = nuevoId > 0;
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
            if (hayCambios)
            {
                var resultado = MessageBox.Show(
                    "Hay cambios sin guardar. ¿Esta seguro de que desea salir?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }
    }
}
