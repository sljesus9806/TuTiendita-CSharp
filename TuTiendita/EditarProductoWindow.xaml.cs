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
    /// Interaction logic for EditarProductoWindow.xaml
    /// </summary>
    public partial class EditarProductoWindow : Window
    {
        public Producto Producto { get; set; }
        private decimal precioOriginal;
        private int stockOriginal;

        public EditarProductoWindow(Producto producto)
        {
            InitializeComponent();

            if (producto == null)
            {
                MessageBox.Show("Error: No se recibio un producto valido.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            Producto = producto;
            precioOriginal = producto.Precio;
            stockOriginal = producto.Stock;

            // Rellenar los campos con los datos del producto
            txtCodigo.Text = Producto.Codigo;
            txtNombre.Text = Producto.Nombre;
            txtPrecio.Text = Producto.Precio.ToString("F2");
            txtStock.Text = Producto.Stock.ToString();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar nombre
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre del producto es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            // Validar precio
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                MessageBox.Show("El precio debe ser un numero valido.\nEjemplo: 99.99", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecio.Focus();
                return;
            }

            if (precio < 0)
            {
                MessageBox.Show("El precio no puede ser negativo.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrecio.Focus();
                return;
            }

            if (precio == 0)
            {
                var resultado = MessageBox.Show("El precio es $0.00. Esta seguro de continuar?",
                    "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (resultado != MessageBoxResult.Yes)
                {
                    txtPrecio.Focus();
                    return;
                }
            }

            // Validar stock
            if (!int.TryParse(txtStock.Text, out int stock))
            {
                MessageBox.Show("El stock debe ser un numero entero valido.\nEjemplo: 50", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return;
            }

            if (stock < 0)
            {
                MessageBox.Show("El stock no puede ser negativo.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return;
            }

            // Advertir si hay cambios significativos en el precio
            if (precio != precioOriginal)
            {
                decimal porcentajeCambio = precioOriginal > 0
                    ? Math.Abs((precio - precioOriginal) / precioOriginal * 100)
                    : 100;

                if (porcentajeCambio > 50)
                {
                    var resultado = MessageBox.Show(
                        $"El precio cambio significativamente ({porcentajeCambio:F1}%).\n" +
                        $"Precio anterior: {precioOriginal:C}\n" +
                        $"Precio nuevo: {precio:C}\n\n" +
                        "Desea continuar?",
                        "Advertencia de Cambio de Precio",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            try
            {
                // Actualizar los valores del producto
                Producto.Nombre = txtNombre.Text.Trim();
                Producto.Precio = precio;
                Producto.Stock = stock;

                // Guardar cambios en la base de datos usando el método EditarProducto
                Producto.EditarProducto(Producto);

                DialogResult = true;  // Cierra la ventana y devuelve un resultado positivo
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar los cambios: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Cierra la ventana sin guardar cambios
        }
    }
}
