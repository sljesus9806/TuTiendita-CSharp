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
            var categorias = Categoria.ObtenerTodas();
            cmbCategoria.ItemsSource = categorias;
            if (categorias.Count > 0)
            {
                cmbCategoria.SelectedIndex = 0;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar los datos ingresados
            if (string.IsNullOrWhiteSpace(txtCodigo.Text) ||
                string.IsNullOrWhiteSpace(txtNombre.Text) ||
                !decimal.TryParse(txtPrecio.Text, out decimal precio) ||
                !decimal.TryParse(txtCosto.Text, out decimal costo) ||
                !int.TryParse(txtStock.Text, out int stock) ||
                !int.TryParse(txtStockMinimo.Text, out int stockMinimo))
            {
                MessageBox.Show("Por favor ingrese datos válidos en todos los campos.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (precio < costo)
            {
                var result = MessageBox.Show("El precio de venta es menor que el costo. ¿Desea continuar?",
                                           "Advertencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return;
            }

            // Crear un nuevo producto
            Producto = new Producto
            {
                Codigo = txtCodigo.Text,
                Nombre = txtNombre.Text,
                Precio = precio,
                Costo = costo,
                Stock = stock,
                StockMinimo = stockMinimo,
                CategoriaId = cmbCategoria.SelectedValue as int?
            };

            DialogResult = true; // Cierra el dialogo y retorna el resultado
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cierra el dialogo sin guardar
            Close();
        }
    }
}
