using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaOrdenCompra : Window
    {
        private Proveedor proveedorActual;
        private Usuario usuarioActual;
        private List<ProductosUserControl.Producto> productosDisponibles;
        private ObservableCollection<DetalleOrdenCompraTemp> detallesOrden;

        public VentanaOrdenCompra(Proveedor proveedor, Usuario usuario)
        {
            InitializeComponent();
            proveedorActual = proveedor;
            usuarioActual = usuario;

            txtProveedor.Text = $"Proveedor: {proveedor.Nombre} | RFC: {proveedor.RFC ?? "N/A"}";

            detallesOrden = new ObservableCollection<DetalleOrdenCompraTemp>();
            dgDetalles.ItemsSource = detallesOrden;

            CargarProductos();
        }

        private void CargarProductos()
        {
            try
            {
                productosDisponibles = ProductosUserControl.Producto.ObtenerTodos();

                cmbProductos.ItemsSource = productosDisponibles;
                cmbProductos.DisplayMemberPath = "NombreConCodigo";
                cmbProductos.SelectedValuePath = "Codigo";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar productos: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProductos.SelectedItem is ProductosUserControl.Producto producto)
            {
                // Pre-fill with product cost
                txtPrecio.Text = producto.Costo.ToString();
                txtCantidad.Focus();
                txtCantidad.SelectAll();
            }
        }

        private void TxtPrecio_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularSubtotal();
        }

        private void CalcularSubtotal()
        {
            if (int.TryParse(txtCantidad.Text, out int cantidad) &&
                decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                decimal subtotal = cantidad * precio;
                txtSubtotal.Text = subtotal.ToString("C");
            }
            else
            {
                txtSubtotal.Text = "$0.00";
            }
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProductos.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un producto.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidad.Focus();
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio < 0)
            {
                MessageBox.Show("Ingrese un precio válido.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecio.Focus();
                return;
            }

            var producto = (ProductosUserControl.Producto)cmbProductos.SelectedItem;

            // Check if product already exists in the order
            var detalleExistente = detallesOrden.FirstOrDefault(d => d.ProductoCodigo == producto.Codigo);
            if (detalleExistente != null)
            {
                detalleExistente.Cantidad += cantidad;
                detalleExistente.Subtotal = detalleExistente.Cantidad * detalleExistente.PrecioUnitario;
                dgDetalles.Items.Refresh();
            }
            else
            {
                var nuevoDetalle = new DetalleOrdenCompraTemp
                {
                    ProductoCodigo = producto.Codigo,
                    ProductoNombre = producto.Nombre,
                    Cantidad = cantidad,
                    PrecioUnitario = precio,
                    Subtotal = cantidad * precio
                };

                detallesOrden.Add(nuevoDetalle);
            }

            // Reset form
            cmbProductos.SelectedIndex = -1;
            txtCantidad.Text = "1";
            txtPrecio.Text = "0";
            txtSubtotal.Text = "$0.00";
            cmbProductos.Focus();

            ActualizarTotales();
        }

        private void BtnEliminarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (dgDetalles.SelectedItem is DetalleOrdenCompraTemp detalle)
            {
                detallesOrden.Remove(detalle);
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            decimal total = detallesOrden.Sum(d => d.Subtotal);
            int totalItems = detallesOrden.Sum(d => d.Cantidad);

            txtTotalOrden.Text = total.ToString("C");
            txtTotalItems.Text = $"{totalItems} items";
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (detallesOrden.Count == 0)
            {
                MessageBox.Show("Agregue al menos un producto a la orden.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                decimal total = detallesOrden.Sum(d => d.Subtotal);

                var orden = new OrdenCompra
                {
                    ProveedorId = proveedorActual.Id,
                    FechaOrden = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Total = total,
                    Estado = "Pendiente",
                    UsuarioId = usuarioActual.IdUsuario
                };

                var detalles = detallesOrden.Select(d => new DetalleOrdenCompra
                {
                    ProductoCodigo = d.ProductoCodigo,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList();

                int ordenId = ProveedoresHelper.CrearOrdenCompra(orden, detalles, usuarioActual);

                if (ordenId > 0)
                {
                    var resultado = MessageBox.Show(
                        $"Orden de compra #{ordenId} creada correctamente.\n\n" +
                        $"Total: {total:C}\n" +
                        $"Estado: Pendiente\n\n" +
                        $"¿Desea marcar la orden como recibida ahora?",
                        "Éxito",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        if (ProveedoresHelper.RecibirOrdenCompra(ordenId, usuarioActual))
                        {
                            MessageBox.Show(
                                "Orden recibida correctamente.\nEl inventario ha sido actualizado.",
                                "Éxito",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error al crear la orden de compra.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear la orden: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (detallesOrden.Count > 0)
            {
                var resultado = MessageBox.Show(
                    "¿Está seguro de que desea cancelar?\nSe perderán todos los datos ingresados.",
                    "Confirmar Cancelación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }
    }

    // Temporary class for order details in the UI
    public class DetalleOrdenCompraTemp
    {
        public string ProductoCodigo { get; set; }
        public string ProductoNombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        public string PrecioUnitarioFormateado => PrecioUnitario.ToString("C");
        public string SubtotalFormateado => Subtotal.ToString("C");
    }
}
