using System;
using System.Windows;
using System.Windows.Input;
using static TuTiendita.ProductosUserControl;

namespace TuTiendita
{
    public partial class AgregarProductoWindow : Window
    {
        public Producto Producto { get; private set; }

        public AgregarProductoWindow()
        {
            InitializeComponent();
            CargarCategorias();

            // Dar foco al campo de codigo para escaneo inmediato
            Loaded += (s, e) => txtCodigo.Focus();

            // Calcular margen cuando cambian los precios
            txtPrecio.TextChanged += (s, e) => CalcularMargen();
            txtCosto.TextChanged += (s, e) => CalcularMargen();
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

        private void TxtCodigo_KeyDown(object sender, KeyEventArgs e)
        {
            // Cuando se presiona Enter (tipico de escaner), avanzar al siguiente campo
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                e.Handled = true;
                txtNombre.Focus();
            }
        }

        private void BtnGenerarCodigo_Click(object sender, RoutedEventArgs e)
        {
            // Generar codigo unico basado en timestamp
            string codigo = $"P{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
            txtCodigo.Text = codigo;
            txtNombre.Focus();
        }

        private void CalcularMargen()
        {
            if (decimal.TryParse(txtPrecio.Text, out decimal precio) &&
                decimal.TryParse(txtCosto.Text, out decimal costo) &&
                costo > 0)
            {
                decimal margen = ((precio - costo) / costo) * 100;
                if (margen < 0)
                {
                    txtMargen.Text = $"{margen:F1}% (Perdida)";
                    txtMargen.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (margen == 0)
                {
                    txtMargen.Text = "0% (Sin ganancia)";
                    txtMargen.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    txtMargen.Text = $"{margen:F1}%";
                    txtMargen.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
                }
            }
            else
            {
                txtMargen.Text = "--";
                txtMargen.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
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

                DialogResult = true;
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
            DialogResult = false;
            Close();
        }
    }
}
