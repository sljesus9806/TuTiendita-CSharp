using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class ClientesUserControl : UserControl
    {
        private Usuario usuarioActual;
        private List<Cliente> clientesActuales;

        public ClientesUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            this.Loaded += ClientesUserControl_Loaded;
        }

        private void ClientesUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarClientes();
        }

        private void CargarClientes()
        {
            try
            {
                clientesActuales = ClientesHelper.ObtenerClientesActivos();
                dgClientes.ItemsSource = clientesActuales;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (clientesActuales == null) return;

            string busqueda = txtBuscar.Text.ToLower();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                dgClientes.ItemsSource = clientesActuales;
            }
            else
            {
                var clientesFiltrados = clientesActuales.Where(c =>
                    c.NombreCompleto.ToLower().Contains(busqueda) ||
                    (c.Documento?.ToLower().Contains(busqueda) ?? false) ||
                    (c.Telefono?.ToLower().Contains(busqueda) ?? false) ||
                    (c.Email?.ToLower().Contains(busqueda) ?? false)
                ).ToList();

                dgClientes.ItemsSource = clientesFiltrados;
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarClientes();
        }

        private void BtnNuevoCliente_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VentanaEditarCliente(null, usuarioActual);
            if (ventana.ShowDialog() == true)
            {
                CargarClientes();
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int clienteId)
            {
                var cliente = ClientesHelper.ObtenerClientePorId(clienteId);
                if (cliente != null)
                {
                    var ventana = new VentanaEditarCliente(cliente, usuarioActual);
                    if (ventana.ShowDialog() == true)
                    {
                        CargarClientes();
                    }
                }
            }
        }

        private void BtnCredito_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int clienteId)
            {
                var cliente = ClientesHelper.ObtenerClientePorId(clienteId);
                if (cliente != null)
                {
                    var ventana = new VentanaCreditoCliente(cliente, usuarioActual);
                    ventana.ShowDialog();
                    CargarClientes(); // Refresh to show updated debt
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int clienteId)
            {
                var cliente = ClientesHelper.ObtenerClientePorId(clienteId);
                if (cliente == null) return;

                var resultado = MessageBox.Show(
                    $"¿Está seguro de que desea eliminar al cliente '{cliente.NombreCompleto}'?\n\n" +
                    $"El cliente será desactivado pero se mantendrá su historial de compras.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado == MessageBoxResult.Yes)
                {
                    if (ClientesHelper.EliminarCliente(clienteId, usuarioActual))
                    {
                        MessageBox.Show("Cliente eliminado correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        CargarClientes();
                    }
                    else
                    {
                        MessageBox.Show("Error al eliminar el cliente.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Add any selection-based behavior here
        }
    }
}
