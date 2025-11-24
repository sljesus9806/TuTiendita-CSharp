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
        private Usuario usuarioActual;

        public ProductosUserControl(Usuario usuario = null) //Principal
        {
            InitializeComponent();
            usuarioActual = usuario;
            CargarProductos();  // Cargar productos desde la base de datos al inicializar el control

            // Configurar permisos después de que todos los controles estén cargados
            this.Loaded += ProductosUserControl_Loaded;
        }

        private void ProductosUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigurarPermisos();
        }

        private void ConfigurarPermisos()
        {
            // Si el usuario es Cajero, deshabilitar eliminación de productos
            if (usuarioActual != null && usuarioActual.NivelAcceso == "Cajero")
            {
                // Ocultar botón de eliminar para cajeros
                btnEliminar.Visibility = Visibility.Collapsed;
            }
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
            AplicarFiltros();
        }

        private void FiltrosChanged(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string textoBusqueda = txtBuscar?.Text?.ToLower() ?? "";
            bool soloStockBajo = chkStockBajo?.IsChecked ?? false;

            var productosFiltrados = productos
                .Where(p => (string.IsNullOrEmpty(textoBusqueda) ||
                           p.Codigo.ToLower().Contains(textoBusqueda) ||
                           p.Nombre.ToLower().Contains(textoBusqueda)) &&
                          (!soloStockBajo || p.StockBajo || p.SinStock))
                .ToList();

            dgProductos.ItemsSource = productosFiltrados;
        }

        private void BtnExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"Productos_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var writer = new System.IO.StreamWriter(saveDialog.FileName))
                    {
                        writer.WriteLine("Codigo,Nombre,Precio,Costo,Stock,StockMinimo,CategoriaId");
                        foreach (var p in productos)
                        {
                            writer.WriteLine($"{p.Codigo},{p.Nombre},{p.Precio},{p.Costo},{p.Stock},{p.StockMinimo},{p.CategoriaId}");
                        }
                    }
                    MessageBox.Show("Productos exportados exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportarCSV_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidad de importación CSV próximamente disponible.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
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
            private int _stock;
            private int _stockMinimo = 5;

            public int Id { get; set; }
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

            public int Stock
            {
                get { return _stock; }
                set
                {
                    if (_stock != value)
                    {
                        _stock = value;
                        OnPropertyChanged(nameof(Stock));
                        OnPropertyChanged(nameof(StockBajo));
                        OnPropertyChanged(nameof(SinStock));
                    }
                }
            }

            public int StockMinimo
            {
                get { return _stockMinimo; }
                set
                {
                    if (_stockMinimo != value)
                    {
                        _stockMinimo = value;
                        OnPropertyChanged(nameof(StockMinimo));
                        OnPropertyChanged(nameof(StockBajo));
                    }
                }
            }

            public decimal Costo { get; set; }
            public int? CategoriaId { get; set; }

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

            // Indicadores de stock
            public bool StockBajo => Stock > 0 && Stock <= StockMinimo;
            public bool SinStock => Stock <= 0;

            // Propiedades formateadas para reportes
            public string PrecioFormateado => Precio.ToString("C");
            public string ValorTotalFormateado => (Precio * Stock).ToString("C");
            public string NombreConCodigo => $"{Codigo} - {Nombre}";

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
                    string query = @"INSERT INTO [Productos] (Codigo, Nombre, Precio, Costo, Stock, StockMinimo, CategoriaId)
                                    VALUES (@Codigo, @Nombre, @Precio, @Costo, @Stock, @StockMinimo, @CategoriaId)";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Costo", producto.Costo);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
                        cmd.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId.HasValue ? (object)producto.CategoriaId.Value : DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            public static void EditarProducto(Producto producto) //Funcion para editar productos que ya existen en base a su codigo
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"UPDATE [Productos]
                                    SET Nombre = @Nombre,
                                        Precio = @Precio,
                                        Costo = @Costo,
                                        Stock = @Stock,
                                        StockMinimo = @StockMinimo,
                                        CategoriaId = @CategoriaId
                                    WHERE Codigo = @Codigo";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", producto.Codigo);
                        cmd.Parameters.AddWithValue("@Nombre", producto.Nombre);
                        cmd.Parameters.AddWithValue("@Precio", producto.Precio);
                        cmd.Parameters.AddWithValue("@Costo", producto.Costo);
                        cmd.Parameters.AddWithValue("@Stock", producto.Stock);
                        cmd.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
                        cmd.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId.HasValue ? (object)producto.CategoriaId.Value : DBNull.Value);
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
                                    Stock = Convert.ToInt32(reader["Stock"]),
                                    Costo = reader["Costo"] != DBNull.Value ? Convert.ToDecimal(reader["Costo"]) : 0,
                                    StockMinimo = reader["StockMinimo"] != DBNull.Value ? Convert.ToInt32(reader["StockMinimo"]) : 5,
                                    CategoriaId = reader["CategoriaId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["CategoriaId"]) : null
                                });
                            }
                        }
                    }
                }
                return productos;
            }

            public static Producto ObtenerPorCodigo(string codigo)
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Productos WHERE Codigo = @codigo";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@codigo", codigo);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Producto
                                {
                                    Codigo = reader["Codigo"].ToString(),
                                    Nombre = reader["Nombre"].ToString(),
                                    Precio = Convert.ToDecimal(reader["Precio"]),
                                    Stock = Convert.ToInt32(reader["Stock"]),
                                    Costo = reader["Costo"] != DBNull.Value ? Convert.ToDecimal(reader["Costo"]) : 0,
                                    StockMinimo = reader["StockMinimo"] != DBNull.Value ? Convert.ToInt32(reader["StockMinimo"]) : 5,
                                    CategoriaId = reader["CategoriaId"] != DBNull.Value ? (int?)Convert.ToInt32(reader["CategoriaId"]) : null
                                };
                            }
                        }
                    }
                }
                return null;
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

        public class Categoria
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }

            public static List<Categoria> ObtenerTodas()
            {
                var categorias = new List<Categoria>();
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Categorias ORDER BY Nombre";
                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categorias.Add(new Categoria
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
                return categorias;
            }
        }
    }
}
