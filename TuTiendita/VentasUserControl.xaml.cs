using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static TuTiendita.ProductosUserControl;

namespace TuTiendita
{
    /// <summary>
    /// Interaction logic for VentasUserControl.xaml
    /// </summary>
    public partial class VentasUserControl : UserControl
    {
        private List<Producto> productosDisponibles; // Lista completa de productos en ventas
        private List<Producto> productosSeleccionados; // Lista de productos seleccionados para la venta



        public VentasUserControl()
        {

            InitializeComponent();
            productosDisponibles = new List<Producto>();  // Inicializa la lista de productos disponibles
            productosSeleccionados = new List<Producto>(); // Inicializa la lista de productos seleccionados
            CargarProductos();  // Carga todos los productos al inicializar

        }
  
        private void CargarProductos()
        {

            productosDisponibles = Producto.ObtenerTodos();
            dgProductos.ItemsSource = productosDisponibles;
        }


        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            productosSeleccionados.Clear();
            ActualizarDataGridVenta();
            CalcularTotal();
        }




        private void TxtBuscarProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            string textoBusqueda = txtBuscarProducto.Text.ToLower();
            var productosFiltrados = productosDisponibles.Where(p =>
                p.Codigo.ToLower().Contains(textoBusqueda) ||
                p.Nombre.ToLower().Contains(textoBusqueda)).ToList();

            dgProductos.ItemsSource = productosFiltrados;
        }


        private void DgProductos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProductos.SelectedItem != null)
            {
                var producto = (Producto)dgProductos.SelectedItem;

                var productoExistente = productosSeleccionados.FirstOrDefault(p => p.Codigo == producto.Codigo);

                if (productoExistente != null)
                {
                    productoExistente.Cantidad++; // Aumenta la cantidad si el producto ya está en la lista
                }
                else
                {
                    producto.Cantidad = 1;
                    productosSeleccionados.Add(producto);
                }

                ActualizarDataGridVenta();
                CalcularTotal();
            }
        }

        private void ActualizarDataGridVenta()
        {
            dgVenta.ItemsSource = null;
            dgVenta.ItemsSource = productosSeleccionados;
        }

        private void CalcularTotal()
        {
            decimal total = productosSeleccionados.Sum(p => p.Precio * p.Cantidad);
            txtTotal.Text = total.ToString("C");
        }

        private void BtnVender_Click(object sender, RoutedEventArgs e)
        {
            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la venta.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Solicitar monto pagado
            var inputDialog = new DialogoEntrada("Ingrese el monto con el que paga el cliente:");
            if (inputDialog.ShowDialog() == true)
            {
                decimal montoPagado;
                if (decimal.TryParse(inputDialog.InputText, out montoPagado))
                {
                    decimal total = productosSeleccionados.Sum(p => p.Precio * p.Cantidad);
                    decimal cambio = montoPagado - total;

                    if (cambio < 0)
                    {
                        MessageBox.Show("El monto pagado es insuficiente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Registrar la venta y actualizar el stock
                    foreach (var producto in productosSeleccionados)
                    {
                        Producto.ActualizarStock(producto.Codigo, producto.Cantidad);
                    }

                    // Generar ticket en PDF
                    GenerarTicketPDF(productosSeleccionados, total, montoPagado, cambio);

                    // Limpiar la venta
                    productosSeleccionados.Clear();
                    ActualizarDataGridVenta();
                    CalcularTotal();
                    MessageBox.Show($"Venta completada. Cambio: {cambio:C}", "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Monto inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerarTicketPDF(List<Producto> productos, decimal total, decimal montoPagado, decimal cambio)
        {
            // Aquí iría el código para generar un PDF
            // Podrías usar una biblioteca como iTextSharp para generar el ticket
            // Ejemplo:
            // - Crear un documento PDF
            // - Añadir el detalle de la venta (productos, total, monto pagado, cambio)
            // - Guardar el documento en el sistema de archivos
        }

    }
}
