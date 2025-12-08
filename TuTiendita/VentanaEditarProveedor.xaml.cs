using System;
using System.Text.RegularExpressions;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaEditarProveedor : Window
    {
        private Proveedor proveedorActual;
        private Usuario usuarioActual;
        private bool esNuevo;
        private bool hayCambios = false;

        public VentanaEditarProveedor(Proveedor proveedor, Usuario usuario)
        {
            InitializeComponent();
            proveedorActual = proveedor;
            usuarioActual = usuario;
            esNuevo = (proveedor == null);

            if (esNuevo)
            {
                txtTitulo.Text = "➕ Nuevo Proveedor";
                proveedorActual = new Proveedor { Activo = true };
            }
            else
            {
                txtTitulo.Text = "✏️ Editar Proveedor";
                CargarDatosProveedor();
            }

            // Registrar cambios en los campos
            txtNombre.TextChanged += (s, e) => hayCambios = true;
            txtRUC.TextChanged += (s, e) => hayCambios = true;
            txtContacto.TextChanged += (s, e) => hayCambios = true;
            txtTelefono.TextChanged += (s, e) => hayCambios = true;
            txtEmail.TextChanged += (s, e) => hayCambios = true;
            txtDireccion.TextChanged += (s, e) => hayCambios = true;
            txtNotas.TextChanged += (s, e) => hayCambios = true;
        }

        private void CargarDatosProveedor()
        {
            if (proveedorActual == null) return;

            txtNombre.Text = proveedorActual.Nombre;
            txtRUC.Text = proveedorActual.RFC;
            txtContacto.Text = proveedorActual.Contacto;
            txtTelefono.Text = proveedorActual.Telefono;
            txtEmail.Text = proveedorActual.Email;
            txtDireccion.Text = proveedorActual.Direccion;
            txtNotas.Text = proveedorActual.Notas;
        }

        private bool ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email no es requerido

            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private bool ValidarRFC(string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
                return true; // RFC no es requerido

            // RFC mexicano: 12 caracteres para personas morales, 13 para fisicas
            // Formato basico: 3-4 letras + 6 digitos fecha + 3 caracteres homoclave
            string rfcLimpio = rfc.Replace("-", "").Replace(" ", "").ToUpper();

            if (rfcLimpio.Length < 12 || rfcLimpio.Length > 13)
                return false;

            // Validacion basica: primeras letras, despues numeros
            string pattern = @"^[A-Z&Ñ]{3,4}\d{6}[A-Z0-9]{3}$";
            return Regex.IsMatch(rfcLimpio, pattern);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre de la empresa es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            // Validar RFC
            string rfc = txtRUC.Text.Trim();
            if (!string.IsNullOrWhiteSpace(rfc) && !ValidarRFC(rfc))
            {
                MessageBox.Show("El formato del RFC no es valido.\n" +
                    "Formato esperado: 3-4 letras + 6 digitos + 3 caracteres\n" +
                    "Ejemplo: ABC123456XYZ", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtRUC.Focus();
                return;
            }

            // Validar RFC duplicado
            if (!string.IsNullOrWhiteSpace(rfc))
            {
                var proveedorExistente = ProveedoresHelper.BuscarPorRFC(rfc);
                if (proveedorExistente != null && (esNuevo || proveedorExistente.Id != proveedorActual.Id))
                {
                    MessageBox.Show($"Ya existe un proveedor con el RFC '{rfc}'.\n" +
                        $"Proveedor: {proveedorExistente.Nombre}", "RFC Duplicado",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtRUC.Focus();
                    return;
                }
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

            // Advertir si no hay contacto
            if (string.IsNullOrWhiteSpace(txtContacto.Text) && string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                var resultado = MessageBox.Show(
                    "No se ha especificado un contacto ni telefono.\n" +
                    "¿Desea continuar sin informacion de contacto?",
                    "Advertencia",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                {
                    txtContacto.Focus();
                    return;
                }
            }

            try
            {
                // Actualizar datos del proveedor
                proveedorActual.Nombre = txtNombre.Text.Trim();
                proveedorActual.RFC = rfc;
                proveedorActual.Contacto = txtContacto.Text.Trim();
                proveedorActual.Telefono = txtTelefono.Text.Trim();
                proveedorActual.Email = email;
                proveedorActual.Direccion = txtDireccion.Text.Trim();
                proveedorActual.Notas = txtNotas.Text.Trim();

                bool exito;
                if (esNuevo)
                {
                    int nuevoId = ProveedoresHelper.CrearProveedor(proveedorActual, usuarioActual);
                    exito = nuevoId > 0;
                }
                else
                {
                    exito = ProveedoresHelper.ActualizarProveedor(proveedorActual, usuarioActual);
                }

                if (exito)
                {
                    MessageBox.Show(
                        esNuevo ? "Proveedor creado correctamente." : "Proveedor actualizado correctamente.",
                        "Exito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error al guardar el proveedor.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el proveedor: {ex.Message}", "Error",
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
