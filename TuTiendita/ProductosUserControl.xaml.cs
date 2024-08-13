using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
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


namespace TuTiendita
{
    /// <summary>
    /// Interaction logic for ProductosUserControl.xaml
    /// </summary>
    public partial class ProductosUserControl : UserControl
    {
        private List<Producto> productos;
        
        public ProductosUserControl() //Principal
        {
            InitializeComponent();
            CargarProductos();  // Cargar productos desde la base de datos al inicializar el control
        }



        private void CargarProductos()
        {
            productos = Producto.ObtenerTodos();  // Carga los productos desde la base de datos
            dgProductos.ItemsSource = productos;  // Actualiza el DataGrid con los productos
        }

        private void ActualizarDataGrid()
        {
            dgProductos.ItemsSource = null;
            dgProductos.ItemsSource = productos;
        }



        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {

            var agregarProductoWindow = new AgregarProductoWindow();

            if (agregarProductoWindow.ShowDialog() == true)
            {
                Producto nuevoProducto = agregarProductoWindow.Producto;
                Producto.AgregarProducto(nuevoProducto); // Guardar en la base de datos
                CargarProductos(); // Refrescar el DataGrid
            }

        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {

            // Verifica que un producto esté seleccionado en el DataGrid
            if (dgProductos.SelectedItem != null)
            {
                // Obtén el producto seleccionado
                var producto = (Producto)dgProductos.SelectedItem;

                // Crea una instancia de la ventana de edición, pasando el producto seleccionado
                var editarProductoWindow = new EditarProductoWindow(producto);

                // Muestra la ventana de edición como un diálogo modal
                if (editarProductoWindow.ShowDialog() == true)
                {
                    // Si el diálogo se cierra con "true", significa que se guardaron los cambios
                    CargarProductos(); // Refresca la lista de productos en el DataGrid
                }
            }
            else
            {
                // Muestra un mensaje si no se ha seleccionado ningún producto
                MessageBox.Show("Seleccione un producto para editar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }



        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgProductos.SelectedItem != null)
            {
                var producto = (Producto)dgProductos.SelectedItem;

                MessageBoxResult result = MessageBox.Show($"¿Está seguro de que desea eliminar el producto {producto.Nombre}?",
                                                          "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    productos.Remove(producto);
                    Producto.EliminarProducto(producto);

                    ActualizarDataGrid();
                }
            }
            else
            {
                MessageBox.Show("Seleccione un producto para eliminar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string textoBusqueda = txtBuscar.Text.ToLower();

            var productosFiltrados = productos
                .Where(p => p.Codigo.ToLower().Contains(textoBusqueda) || p.Nombre.ToLower().Contains(textoBusqueda))
                .ToList();

            dgProductos.ItemsSource = productosFiltrados;
        }

        public static bool ExisteProductoConCodigo(string codigo) //validacion para textbox de busqueda
        {
            using (var connection = Database.GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM [Productos] WHERE Codigo = @Codigo";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    var count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


        public class Producto : INotifyPropertyChanged
        {
            private int _cantidad;
            private decimal _precio;

            public string Codigo { get; set; }
            public string Nombre { get; set; }
            public decimal Precio
            {
                get { return _precio; }
                set
                {
                    if (_precio != value)
                    {
                        _precio = value;
                        OnPropertyChanged(nameof(Precio));
                        OnPropertyChanged(nameof(Subtotal)); // Actualiza el subtotal cuando cambie el precio
                    }
                }
            }
            public int Stock { get; set; }

            public int Cantidad
            {
                get { return _cantidad; }
                set
                {
                    if (_cantidad != value)
                    {
                        _cantidad = value;
                        OnPropertyChanged(nameof(Cantidad)); // Notificar el cambio en cantidad
                        OnPropertyChanged(nameof(Subtotal)); // Notificar que el subtotal ha cambiado
                    }
                }
            }

            // Propiedad calculada para el Subtotal
            public decimal Subtotal
            {
                get
                {
                    return Precio * Cantidad;
                }
            }

            // Implementación de INotifyPropertyChanged
            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        


        //CRUD Operaciones

        public static void AgregarProducto(Producto producto) //Funcion para agregar productos nuevos

            {
                if (ExisteProductoConCodigo(producto.Codigo))
                {
                    MessageBox.Show("El código del producto ya existe. Por favor, ingrese un código único.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Salir de la función si el código ya existe
                }


                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO [Productos] (Codigo, Nombre, Precio, Stock) VALUES (@Codigo, @Nombre, @Precio, @Stock)";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.ExecuteNonQuery();

                    }



                }
            }

            public static void EditarProducto(Producto producto) //Funcion para editar productos que ya existen en base a su codigo

            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE [Productos] SET Nombre = @Nombre, Precio = @Precio, Stock = @Stock WHERE Codigo = @Codigo";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.ExecuteNonQuery();

                    }
                }



            }
            public static void EliminarProducto(Producto producto)
            {

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM [Productos] WHERE Codigo = @Codigo";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                        cmd.ExecuteNonQuery();

                    }

                }

            }
            public static List<Producto> ObtenerTodos()
            {
                var productos = new List<Producto>();
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Productos";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productos.Add(new Producto
                                {
                                    Codigo = reader["Codigo"].ToString(),
                                    Nombre = reader["Nombre"].ToString(),
                                    Precio = Convert.ToDecimal(reader["Precio"]),
                                    Stock = Convert.ToInt32(reader["Stock"])
                                });
                            }
                        }
                    }
                }
                return productos;




            }


            public static void ActualizarStock(string codigo, int cantidadVendida)
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE [Productos] SET Stock = Stock - @CantidadVendida WHERE Codigo = @Codigo";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@CantidadVendida", cantidadVendida);
                        cmd.Parameters.AddWithValue("@Codigo", codigo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }




        }
    }
}
