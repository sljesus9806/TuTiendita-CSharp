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
    /// Interaction logic for AgregarProductoWindow.xaml
    /// </summary>
    public partial class AgregarProductoWindow : Window
    {
        public Producto Producto { get; private set; }

        public AgregarProductoWindow()
        {
            InitializeComponent();
            CargarCategorias();
        }

        private void CargarCategorias()
        {
            try
            {
                var categorias = Categoria.ObtenerTodas();
                cmbCategoria.ItemsSource = categorias;
                if (categorias.Count > 0)
                {
                    cmbCategoria.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar categorias: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar codigo
            if (string.IsNullOrWhiteSpace(txtCodigo.Text))
            {
                MessageBox.Show("El codigo del producto es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCodigo.Focus();
                return;
            }

            // Verificar si el codigo ya existe
            string codigoLimpio = txtCodigo.Text.Trim();
            var productoExistente = Producto.ObtenerPorCodigo(codigoLimpio);
            if (productoExistente != null)
            {
                MessageBox.Show($"Ya existe un producto con el codigo '{codigoLimpio}'.\n" +
                    $"Producto existente: {productoExistente.Nombre}",
                    "Codigo Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                txtCodigo.Focus();
                return;
            }

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

            // Validar costo
            if (!decimal.TryParse(txtCosto.Text, out decimal costo))
            {
                MessageBox.Show("El costo debe ser un numero valido.\nEjemplo: 50.00", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCosto.Focus();
                return;
            }

            if (costo < 0)
            {
                MessageBox.Show("El costo no puede ser negativo.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCosto.Focus();
                return;
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

            // Validar stock minimo
            if (!int.TryParse(txtStockMinimo.Text, out int stockMinimo))
            {
                MessageBox.Show("El stock minimo debe ser un numero entero valido.\nEjemplo: 5", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStockMinimo.Focus();
                return;
            }

            if (stockMinimo < 0)
            {
                MessageBox.Show("El stock minimo no puede ser negativo.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStockMinimo.Focus();
                return;
            }

            // Advertencia: Precio menor que costo
            if (precio < costo)
            {
                var result = MessageBox.Show(
                    $"El precio de venta ({precio:C}) es menor que el costo ({costo:C}).\n" +
                    "Esto significa que perderas dinero en cada venta.\n\n" +
                    "Desea continuar de todos modos?",
                    "Advertencia de Margen Negativo",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    txtPrecio.Focus();
                    return;
                }
            }

            // Advertencia: Precio igual a costo (sin margen)
            if (precio == costo && precio > 0)
            {
                var result = MessageBox.Show(
                    $"El precio de venta es igual al costo ({precio:C}).\n" +
                    "No tendras margen de ganancia.\n\n" +
                    "Desea continuar?",
                    "Advertencia de Margen Cero",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    txtPrecio.Focus();
                    return;
                }
            }

            // Advertencia: Stock inicial en 0
            if (stock == 0)
            {
                var result = MessageBox.Show(
                    "El stock inicial es 0.\n" +
                    "El producto no estara disponible para venta hasta que agregue stock.\n\n" +
                    "Desea continuar?",
                    "Stock Cero",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.No)
                {
                    txtStock.Focus();
                    return;
                }
            }

            try
            {
                // Crear un nuevo producto
                Producto = new Producto
                {
                    Codigo = codigoLimpio,
                    Nombre = txtNombre.Text.Trim(),
                    Precio = precio,
                    Costo = costo,
                    Stock = stock,
                    StockMinimo = stockMinimo,
                    CategoriaId = cmbCategoria.SelectedValue as int?
                };

                DialogResult = true; // Cierra el dialogo y retorna el resultado
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el producto: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cierra el dialogo sin guardar
            Close();
        }
    }
}
