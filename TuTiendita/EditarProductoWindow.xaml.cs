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

        public EditarProductoWindow(Producto producto)
        {
            InitializeComponent();
            Producto = producto;

            // Rellenar los campos con los datos del producto
            txtCodigo.Text = Producto.Codigo;
            txtNombre.Text = Producto.Nombre;
            txtPrecio.Text = Producto.Precio.ToString();
            txtStock.Text = Producto.Stock.ToString();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Actualizar los valores del producto
            Producto.Nombre = txtNombre.Text;
            Producto.Precio = decimal.Parse(txtPrecio.Text);
            Producto.Stock = int.Parse(txtStock.Text);

            // Guardar cambios en la base de datos usando el método EditarProducto
            Producto.EditarProducto(Producto);
            DialogResult = true;  // Cierra la ventana y devuelve un resultado positivo
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Cierra la ventana sin guardar cambios
        }
    }
}
