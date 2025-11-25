using System;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaEditarProveedor : Window
    {
        private Proveedor proveedorActual;
        private Usuario usuarioActual;
        private bool esNuevo;

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

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos requeridos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre de la empresa es obligatorio.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            try
            {
                // Actualizar datos del proveedor
                proveedorActual.Nombre = txtNombre.Text.Trim();
                proveedorActual.RFC = txtRUC.Text.Trim();
                proveedorActual.Contacto = txtContacto.Text.Trim();
                proveedorActual.Telefono = txtTelefono.Text.Trim();
                proveedorActual.Email = txtEmail.Text.Trim();
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
                        "Éxito",
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
            DialogResult = false;
            Close();
        }
    }
}
