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

            // Calcular total con redondeo a 2 decimales para evitar errores de centavos
            decimal total = Math.Round(productosSeleccionados.Sum(p => Math.Round(p.Precio * p.Cantidad, 2)), 2);

            // Validar que el total sea razonable (prevenir errores de dedo)
            if (total > 50000)
            {
                var confirmar = MessageBox.Show(
                    $"El total de {total:C} parece muy alto.\n¿Está seguro que es correcto?",
                    "Confirmar Venta", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirmar != MessageBoxResult.Yes)
                    return;
            }

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
                var pagoDialog = new DialogoPagoEfectivo(total);
                if (pagoDialog.ShowDialog() == true)
                {
                    montoPagado = pagoDialog.MontoRecibido;
                    cambio = pagoDialog.Cambio;
                }
                else
                {
                    return; // Usuario cancelo
                }
            }
            else
            {
                // Para tarjeta y transferencia, monto pagado = total
                montoPagado = total;
                cambio = 0;
            }

            // IMPORTANTE: Guardar venta Y actualizar stock en UNA SOLA transacción atómica
            // Esto previene inconsistencias si se va la luz o hay errores
            var resultado = GuardarVentaConStock(total, montoPagado, cambio, metodoPago);

            if (resultado.Exito && resultado.VentaId > 0)
            {
                // Generar ticket en PDF (después de confirmar la transacción)
                GenerarTicketPDF(resultado.VentaId, productosSeleccionados, total, montoPagado, cambio, metodoPago);

                // Limpiar la venta
                productosSeleccionados.Clear();
                ActualizarDataGridVenta();
                CalcularTotal();
                CargarProductos(); // Recargar productos para actualizar stock

                string mensaje = $"Venta #{resultado.VentaId} completada.\nMétodo de pago: {metodoPago}";
                if (metodoPago == "Efectivo")
                {
                    mensaje += $"\nCambio: {cambio:C}";
                }
                MessageBox.Show(mensaje, "Venta Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (!string.IsNullOrEmpty(resultado.MensajeError))
            {
                MessageBox.Show(resultado.MensajeError, "Error en Venta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Resultado de operación de venta
        /// </summary>
        private class ResultadoVenta
        {
            public bool Exito { get; set; }
            public long VentaId { get; set; }
            public string MensajeError { get; set; }
        }

        /// <summary>
        /// Guarda la venta Y actualiza el stock en una SOLA transacción atómica.
        /// Esto previene inconsistencias si hay cortes de luz o errores.
        /// </summary>
        private ResultadoVenta GuardarVentaConStock(decimal total, decimal montoPagado, decimal cambio, string metodoPago)
        {
            var resultado = new ResultadoVenta();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // PASO 1: Verificar stock Y bloquearlo en la misma transacción
                            foreach (var item in productosSeleccionados)
                            {
                                string stockQuery = "SELECT Stock FROM Productos WHERE Codigo = @codigo";
                                using (var stockCmd = new SQLiteCommand(stockQuery, connection, transaction))
                                {
                                    stockCmd.Parameters.AddWithValue("@codigo", item.Codigo);
                                    var stockActual = stockCmd.ExecuteScalar();

                                    if (stockActual == null || stockActual == DBNull.Value)
                                    {
                                        resultado.MensajeError = $"Producto '{item.Nombre}' no encontrado en inventario.";
                                        transaction.Rollback();
                                        return resultado;
                                    }

                                    int stock = Convert.ToInt32(stockActual);
                                    if (stock < item.Cantidad)
                                    {
                                        resultado.MensajeError = $"Stock insuficiente para '{item.Nombre}'.\nDisponible: {stock}, Solicitado: {item.Cantidad}";
                                        transaction.Rollback();
                                        return resultado;
                                    }
                                }
                            }

                            // PASO 2: Insertar la venta
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

                                ventaId = Convert.ToInt64(cmd.ExecuteScalar());
                            }

                            // PASO 3: Insertar detalles de la venta
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
                                    cmd.Parameters.AddWithValue("@subtotal", Math.Round(producto.Precio * producto.Cantidad, 2));
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // PASO 4: Actualizar stock (DENTRO de la misma transacción)
                            string updateStockQuery = "UPDATE Productos SET Stock = Stock - @cantidad WHERE Codigo = @codigo AND Stock >= @cantidad";
                            foreach (var producto in productosSeleccionados)
                            {
                                using (var cmd = new SQLiteCommand(updateStockQuery, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@cantidad", producto.Cantidad);
                                    cmd.Parameters.AddWithValue("@codigo", producto.Codigo);
                                    int filasAfectadas = cmd.ExecuteNonQuery();

                                    // Verificar que se actualizó (protección extra contra race conditions)
                                    if (filasAfectadas == 0)
                                    {
                                        resultado.MensajeError = $"No se pudo actualizar stock de '{producto.Nombre}'. Posible venta simultánea.";
                                        transaction.Rollback();
                                        return resultado;
                                    }
                                }
                            }

                            // PASO 5: Actualizar totales del turno
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

                            // COMMIT: Todo o nada
                            transaction.Commit();

                            resultado.Exito = true;
                            resultado.VentaId = ventaId;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            resultado.MensajeError = $"Error al procesar venta: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                resultado.MensajeError = $"Error de conexión: {ex.Message}";
            }

            return resultado;
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

        // NOTA: El método GuardarVenta fue reemplazado por GuardarVentaConStock
        // que es más seguro porque incluye la actualización del stock en la misma transacción
    }
}
