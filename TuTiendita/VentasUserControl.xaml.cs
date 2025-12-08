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
            ActualizarEstadoInterfaz(); // Actualiza la interfaz según si hay turno o no
        }
  
        private void CargarProductos()
        {
            productosDisponibles = Producto.ObtenerTodos();
            dgProductos.ItemsSource = productosDisponibles;
        }

        private void BtnEliminarItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is Producto producto)
            {
                productosSeleccionados.Remove(producto);
                ActualizarDataGridVenta();
                CalcularTotal();
            }
        }

        private void BtnAumentarCantidad_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is Producto productoCarrito)
            {
                // Buscar el producto original para verificar stock
                var productoOriginal = productosDisponibles.FirstOrDefault(p => p.Codigo == productoCarrito.Codigo);
                if (productoOriginal != null)
                {
                    if (productoCarrito.Cantidad + 1 > productoOriginal.Stock)
                    {
                        MessageBox.Show($"Stock insuficiente. Stock disponible: {productoOriginal.Stock}",
                                      "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                productoCarrito.Cantidad++;
                ActualizarDataGridVenta();
                CalcularTotal();
            }
        }

        private void BtnDisminuirCantidad_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is Producto productoCarrito)
            {
                if (productoCarrito.Cantidad > 1)
                {
                    productoCarrito.Cantidad--;
                    ActualizarDataGridVenta();
                    CalcularTotal();
                }
                else
                {
                    // Si la cantidad es 1, preguntar si desea eliminar el producto
                    var resultado = MessageBox.Show(
                        $"¿Desea eliminar '{productoCarrito.Nombre}' del carrito?",
                        "Confirmar eliminación",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        productosSeleccionados.Remove(productoCarrito);
                        ActualizarDataGridVenta();
                        CalcularTotal();
                    }
                }
            }
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

                // Validar que haya stock disponible
                if (producto.Stock <= 0)
                {
                    MessageBox.Show($"El producto '{producto.Nombre}' no tiene stock disponible.",
                                  "Sin Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var productoExistente = productosSeleccionados.FirstOrDefault(p => p.Codigo == producto.Codigo);

                if (productoExistente != null)
                {
                    // Verificar que no exceda el stock disponible
                    if (productoExistente.Cantidad + 1 > producto.Stock)
                    {
                        MessageBox.Show($"Stock insuficiente. Stock disponible: {producto.Stock}",
                                      "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    productoExistente.Cantidad++; // Aumenta la cantidad si el producto ya está en la lista
                }
                else
                {
                    // Crear una copia del producto para el carrito
                    var nuevoItem = new Producto
                    {
                        Codigo = producto.Codigo,
                        Nombre = producto.Nombre,
                        Precio = producto.Precio,
                        Stock = producto.Stock,
                        Cantidad = 1
                    };
                    productosSeleccionados.Add(nuevoItem);
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

        private bool ValidarStockDisponible()
        {
            foreach (var item in productosSeleccionados)
            {
                var productoActual = Producto.ObtenerPorCodigo(item.Codigo);
                if (productoActual == null || productoActual.Stock < item.Cantidad)
                {
                    MessageBox.Show($"Stock insuficiente para '{item.Nombre}'.\nStock disponible: {productoActual?.Stock ?? 0}\nCantidad en carrito: {item.Cantidad}",
                                  "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        private void BtnVender_Click(object sender, RoutedEventArgs e)
        {
            // Verificar que hay un turno abierto
            if (!turnoActualId.HasValue)
            {
                MessageBox.Show("No hay un turno abierto. Por favor abra un turno en 'Gestión de Turno' antes de realizar ventas.",
                              "Turno No Iniciado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (productosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay productos en la venta.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar stock antes de proceder
            if (!ValidarStockDisponible())
            {
                return;
            }

            decimal total = productosSeleccionados.Sum(p => p.Precio * p.Cantidad);

            // Seleccionar método de pago
            var metodoPagoDialog = new DialogoMetodoPago();
            if (metodoPagoDialog.ShowDialog() != true)
            {
                return; // Usuario canceló
            }

            string metodoPago = metodoPagoDialog.MetodoPagoSeleccionado;
            decimal montoPagado = 0;
            decimal cambio = 0;

            // Solo pedir monto si es efectivo
            if (metodoPago == "Efectivo")
            {
                var inputDialog = new DialogoEntrada("Ingrese el monto con el que paga el cliente:");
                if (inputDialog.ShowDialog() == true)
                {
                    if (decimal.TryParse(inputDialog.InputText, out montoPagado))
                    {
                        cambio = montoPagado - total;

                        if (cambio < 0)
                        {
                            MessageBox.Show("El monto pagado es insuficiente.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Monto inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    return; // Usuario canceló
                }
            }
            else
            {
                // Para tarjeta y transferencia, monto pagado = total
                montoPagado = total;
                cambio = 0;
            }

            // Guardar la venta en la base de datos
            long ventaId = GuardarVenta(total, montoPagado, cambio, metodoPago);

            if (ventaId > 0)
            {
                // Actualizar el stock
                foreach (var producto in productosSeleccionados)
                {
                    Producto.ActualizarStock(producto.Codigo, producto.Cantidad);
                }

                // Generar ticket en PDF
                GenerarTicketPDF(ventaId, productosSeleccionados, total, montoPagado, cambio, metodoPago);

                // Limpiar la venta
                productosSeleccionados.Clear();
                ActualizarDataGridVenta();
                CalcularTotal();
                CargarProductos(); // Recargar productos para actualizar stock

                string mensaje = $"Venta #{ventaId} completada.\nMétodo de pago: {metodoPago}";
                if (metodoPago == "Efectivo")
                {
                    mensaje += $"\nCambio: {cambio:C}";
                }
                MessageBox.Show(mensaje, "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerarTicketPDF(long ventaId, List<Producto> productos, decimal total, decimal montoPagado, decimal cambio, string metodoPago)
        {
            try
            {
                // Crear directorio de tickets si no existe
                string ticketsFolder = "Tickets";
                if (!System.IO.Directory.Exists(ticketsFolder))
                {
                    System.IO.Directory.CreateDirectory(ticketsFolder);
                }

                // Generar nombre de archivo PDF
                string fileName = $"Tickets/Ticket_{ventaId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // Usar el generador de PDF profesional
                bool exito = Helpers.TicketPdfGenerator.GenerarTicketVenta((int)ventaId, fileName);

                if (exito)
                {
                    // Registrar en auditoría
                    Helpers.AuditLogger.RegistrarVenta(usuarioActual, (int)ventaId, total, productos.Count);

                    // Preguntar si desea abrir el ticket
                    var resultado = MessageBox.Show(
                        "✓ Ticket generado exitosamente\n\n¿Desea abrir el ticket?",
                        "Ticket PDF",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        Helpers.TicketPdfGenerator.AbrirPdf(fileName);
                    }
                }
                else
                {
                    MessageBox.Show("Error al generar el ticket PDF", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar ticket: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

        private void ActualizarEstadoInterfaz()
        {
            if (turnoActualId.HasValue)
            {
                // Hay turno abierto - habilitar ventas
                brdEstadoTurno.Visibility = Visibility.Collapsed;
                pnlVentas.IsEnabled = true;
            }
            else
            {
                // No hay turno abierto - deshabilitar ventas y mostrar advertencia
                brdEstadoTurno.Visibility = Visibility.Visible;
                pnlVentas.IsEnabled = false;
            }
        }

        private long GuardarVenta(decimal total, decimal montoPagado, decimal cambio, string metodoPago)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insertar la venta
                            string insertVentaQuery = @"INSERT INTO Ventas (TurnoId, UsuarioId, UsuarioNombre, Fecha, Total, MontoPagado, Cambio, MetodoPago)
                                                       VALUES (@turnoId, @usuarioId, @usuarioNombre, @fecha, @total, @montoPagado, @cambio, @metodoPago);
                                                       SELECT last_insert_rowid();";

                            long ventaId;
                            using (var cmd = new SQLiteCommand(insertVentaQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@turnoId", turnoActualId.HasValue ? (object)turnoActualId.Value : DBNull.Value);
                                cmd.Parameters.AddWithValue("@usuarioId", usuarioActual.IdUsuario);
                                cmd.Parameters.AddWithValue("@usuarioNombre", usuarioActual.Nombre);
                                cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@total", total);
                                cmd.Parameters.AddWithValue("@montoPagado", montoPagado);
                                cmd.Parameters.AddWithValue("@cambio", cambio);
                                cmd.Parameters.AddWithValue("@metodoPago", metodoPago);

                                ventaId = (long)cmd.ExecuteScalar();
                            }

                            // Insertar los detalles de la venta
                            string insertDetalleQuery = @"INSERT INTO DetalleVentas (VentaId, ProductoCodigo, ProductoNombre, Cantidad, PrecioUnitario, Subtotal)
                                                         VALUES (@ventaId, @productoCodigo, @productoNombre, @cantidad, @precioUnitario, @subtotal)";

                            foreach (var producto in productosSeleccionados)
                            {
                                using (var cmd = new SQLiteCommand(insertDetalleQuery, connection, transaction))
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
                                string campoMetodo = metodoPago == "Efectivo" ? "TotalEfectivo" :
                                                   metodoPago == "Tarjeta" ? "TotalTarjeta" : "TotalTransferencia";

                                string updateTurnoQuery = $@"UPDATE Turnos
                                                           SET TotalVentas = TotalVentas + @total,
                                                               {campoMetodo} = {campoMetodo} + @total
                                                           WHERE Id = @turnoId";
                                using (var cmd = new SQLiteCommand(updateTurnoQuery, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@total", total);
                                    cmd.Parameters.AddWithValue("@turnoId", turnoActualId.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return ventaId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
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
