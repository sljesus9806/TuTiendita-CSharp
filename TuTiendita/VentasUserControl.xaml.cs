using System;
using System.Collections.Generic;
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
        private Usuario usuarioActual; // Usuario que realiza la venta
        private int? turnoActualId; // ID del turno actual (si existe uno abierto)

        public VentasUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            productosDisponibles = new List<Producto>();  // Inicializa la lista de productos disponibles
            productosSeleccionados = new List<Producto>(); // Inicializa la lista de productos seleccionados
            CargarProductos();  // Carga todos los productos al inicializar
            ObtenerTurnoActual(); // Obtiene el turno actual si existe
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

                    // Guardar la venta en la base de datos
                    long ventaId = GuardarVenta(total, montoPagado, cambio);

                    if (ventaId > 0)
                    {
                        // Actualizar el stock
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
                        CargarProductos(); // Recargar productos para actualizar stock
                        MessageBox.Show($"Venta #{ventaId} completada. Cambio: {cambio:C}", "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
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

        private void ObtenerTurnoActual()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Id FROM Turnos WHERE Estado = 'Abierto' ORDER BY FechaApertura DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            turnoActualId = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener turno actual: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private long GuardarVenta(decimal total, decimal montoPagado, decimal cambio)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Insertar la venta
                    string insertVentaQuery = @"INSERT INTO Ventas (TurnoId, UsuarioId, UsuarioNombre, Fecha, Total, MontoPagado, Cambio)
                                               VALUES (@turnoId, @usuarioId, @usuarioNombre, @fecha, @total, @montoPagado, @cambio);
                                               SELECT last_insert_rowid();";

                    long ventaId;
                    using (var cmd = new SQLiteCommand(insertVentaQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@turnoId", turnoActualId.HasValue ? (object)turnoActualId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@usuarioId", usuarioActual.IdUsuario);
                        cmd.Parameters.AddWithValue("@usuarioNombre", usuarioActual.Nombre);
                        cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@total", total);
                        cmd.Parameters.AddWithValue("@montoPagado", montoPagado);
                        cmd.Parameters.AddWithValue("@cambio", cambio);

                        ventaId = (long)cmd.ExecuteScalar();
                    }

                    // Insertar los detalles de la venta
                    string insertDetalleQuery = @"INSERT INTO DetalleVentas (VentaId, ProductoCodigo, ProductoNombre, Cantidad, PrecioUnitario, Subtotal)
                                                 VALUES (@ventaId, @productoCodigo, @productoNombre, @cantidad, @precioUnitario, @subtotal)";

                    foreach (var producto in productosSeleccionados)
                    {
                        using (var cmd = new SQLiteCommand(insertDetalleQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@ventaId", ventaId);
                            cmd.Parameters.AddWithValue("@productoCodigo", producto.Codigo);
                            cmd.Parameters.AddWithValue("@productoNombre", producto.Nombre);
                            cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                            cmd.Parameters.AddWithValue("@precioUnitario", producto.Precio);
                            cmd.Parameters.AddWithValue("@subtotal", producto.Precio * producto.Cantidad);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Actualizar el total de ventas del turno si existe
                    if (turnoActualId.HasValue)
                    {
                        string updateTurnoQuery = "UPDATE Turnos SET TotalVentas = TotalVentas + @total WHERE Id = @turnoId";
                        using (var cmd = new SQLiteCommand(updateTurnoQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@total", total);
                            cmd.Parameters.AddWithValue("@turnoId", turnoActualId.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    return ventaId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la venta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

    }
}
