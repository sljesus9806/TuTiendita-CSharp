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
            VerificarPermisos();
        }

        private void VerificarPermisos()
        {
            // Validar que el control este inicializado
            if (btnNuevoProveedor == null)
                return;

            // Solo los gerentes pueden agregar, editar o eliminar proveedores
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                // Deshabilitar boton de nuevo proveedor
                btnNuevoProveedor.IsEnabled = false;
                btnNuevoProveedor.Opacity = 0.5;
                btnNuevoProveedor.ToolTip = "Solo los gerentes pueden agregar proveedores";
            }
        }

        private void CargarProveedores()
        {
            // Validar que el control este inicializado
            if (dgProveedores == null)
                return;

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
            // Validar que los controles esten inicializados
            if (txtBuscar == null || dgProveedores == null || proveedoresActuales == null)
                return;

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
            try
            {
                var ventana = new VentanaCreditosPendientes();
                ventana.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana de creditos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNuevoProveedor_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden agregar nuevos proveedores.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Agregar proveedor");
                return;
            }

            try
            {
                var ventana = new VentanaEditarProveedor(null, usuarioActual);
                if (ventana.ShowDialog() == true)
                {
                    CargarProveedores();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir ventana de nuevo proveedor: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden editar proveedores.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Editar proveedor");
                return;
            }

            if (sender is Button button && button.Tag is int proveedorId)
            {
                try
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
                    else
                    {
                        MessageBox.Show("Proveedor no encontrado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al editar proveedor: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden crear ordenes de compra.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Crear orden de compra");
                return;
            }

            if (sender is Button button && button.Tag is int proveedorId)
            {
                try
                {
                    var proveedor = ProveedoresHelper.ObtenerProveedorPorId(proveedorId);
                    if (proveedor != null)
                    {
                        var ventana = new VentanaOrdenCompra(proveedor, usuarioActual);
                        ventana.ShowDialog();
                        CargarProveedores(); // Refrescar por si cambiaron totales
                    }
                    else
                    {
                        MessageBox.Show("Proveedor no encontrado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear orden de compra: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden eliminar proveedores.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Eliminar proveedor");
                return;
            }

            if (sender is Button button && button.Tag is int proveedorId)
            {
                try
                {
                    var proveedor = ProveedoresHelper.ObtenerProveedorPorId(proveedorId);
                    if (proveedor == null)
                    {
                        MessageBox.Show("Proveedor no encontrado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Verificar si tiene ordenes pendientes
                    var ordenesPendientes = ProveedoresHelper.ObtenerOrdenesCompra(proveedorId)
                        .Count(o => o.Estado == "Pendiente");

                    string mensajeAdvertencia = $"Esta seguro de que desea eliminar al proveedor '{proveedor.Nombre}'?\n\n";

                    if (ordenesPendientes > 0)
                    {
                        mensajeAdvertencia += $"ADVERTENCIA: Este proveedor tiene {ordenesPendientes} orden(es) pendiente(s).\n\n";
                    }

                    mensajeAdvertencia += "El proveedor sera desactivado pero se mantendra su historial de ordenes.";

                    var resultado = MessageBox.Show(
                        mensajeAdvertencia,
                        "Confirmar Eliminacion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        if (ProveedoresHelper.EliminarProveedor(proveedorId, usuarioActual))
                        {
                            MessageBox.Show("Proveedor eliminado correctamente.", "Exito",
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
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar proveedor: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
