using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class ProveedoresUserControl : UserControl
    {
        private Usuario usuarioActual;
        private List<Proveedor> proveedoresActuales;

        public ProveedoresUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            this.Loaded += ProveedoresUserControl_Loaded;
        }

        private void ProveedoresUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarProveedores();
        }

        private void CargarProveedores()
        {
            try
            {
                proveedoresActuales = ProveedoresHelper.ObtenerProveedoresActivos();
                dgProveedores.ItemsSource = proveedoresActuales;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar proveedores: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (proveedoresActuales == null) return;

            string busqueda = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                dgProveedores.ItemsSource = proveedoresActuales;
            }
            else
            {
                var proveedoresFiltrados = proveedoresActuales.Where(p =>
                    p.Nombre.ToLower().Contains(busqueda) ||
                    (p.RFC?.ToLower().Contains(busqueda) ?? false) ||
                    (p.Contacto?.ToLower().Contains(busqueda) ?? false) ||
                    (p.Telefono?.ToLower().Contains(busqueda) ?? false)
                ).ToList();

                dgProveedores.ItemsSource = proveedoresFiltrados;
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarProveedores();
        }

        private void BtnVerCreditos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VentanaCreditosPendientes();
            ventana.ShowDialog();
        }

        private void BtnNuevoProveedor_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VentanaEditarProveedor(null, usuarioActual);
            if (ventana.ShowDialog() == true)
            {
                CargarProveedores();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int proveedorId)
            {
                var proveedor = ProveedoresHelper.ObtenerProveedorPorId(proveedorId);
                if (proveedor != null)
                {
                    var ventana = new VentanaEditarProveedor(proveedor, usuarioActual);
                    if (ventana.ShowDialog() == true)
                    {
                        CargarProveedores();
                    }
                }
            }
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int proveedorId)
            {
                var proveedor = ProveedoresHelper.ObtenerProveedorPorId(proveedorId);
                if (proveedor != null)
                {
                    var ventana = new VentanaOrdenCompra(proveedor, usuarioActual);
                    ventana.ShowDialog();
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int proveedorId)
            {
                var proveedor = ProveedoresHelper.ObtenerProveedorPorId(proveedorId);
                if (proveedor == null) return;

                var resultado = MessageBox.Show(
                    $"¿Está seguro de que desea eliminar al proveedor '{proveedor.Nombre}'?\n\n" +
                    $"El proveedor será desactivado pero se mantendrá su historial de órdenes.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    if (ProveedoresHelper.EliminarProveedor(proveedorId, usuarioActual))
                    {
                        MessageBox.Show("Proveedor eliminado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarProveedores();
                    }
                    else
                    {
                        MessageBox.Show("Error al eliminar el proveedor.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
