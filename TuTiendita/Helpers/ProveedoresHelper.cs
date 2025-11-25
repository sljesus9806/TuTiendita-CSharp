using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Helper para gestionar proveedores y órdenes de compra
    /// </summary>
    public static class ProveedoresHelper
    {
        /// <summary>
        /// Obtiene todos los proveedores activos
        /// </summary>
        public static List<Proveedor> ObtenerProveedoresActivos()
        {
            var proveedores = new List<Proveedor>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT p.*,
                                   COUNT(DISTINCT oc.Id) as TotalOrdenes,
                                   COALESCE(SUM(oc.Total), 0) as TotalCompras
                                   FROM Proveedores p
                                   LEFT JOIN OrdenesCompra oc ON oc.ProveedorId = p.Id
                                   WHERE p.Activo = 1
                                   GROUP BY p.Id
                                   ORDER BY p.Nombre";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                proveedores.Add(new Proveedor
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Contacto = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Telefono = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Direccion = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    RUC = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    Activo = reader.GetInt32(7) == 1,
                                    Notas = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    TotalOrdenes = reader.GetInt32(9),
                                    TotalCompras = reader.GetDecimal(10)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener proveedores: {ex.Message}");
            }

            return proveedores;
        }

        /// <summary>
        /// Obtiene un proveedor por ID
        /// </summary>
        public static Proveedor ObtenerProveedorPorId(int proveedorId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT p.*,
                                   COUNT(DISTINCT oc.Id) as TotalOrdenes,
                                   COALESCE(SUM(oc.Total), 0) as TotalCompras
                                   FROM Proveedores p
                                   LEFT JOIN OrdenesCompra oc ON oc.ProveedorId = p.Id
                                   WHERE p.Id = @Id
                                   GROUP BY p.Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", proveedorId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Proveedor
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Contacto = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Telefono = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Direccion = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    RUC = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    Activo = reader.GetInt32(7) == 1,
                                    Notas = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    TotalOrdenes = reader.GetInt32(9),
                                    TotalCompras = reader.GetDecimal(10)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener proveedor: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Elimina (desactiva) un proveedor
        /// </summary>
        public static bool EliminarProveedor(int proveedorId, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE Proveedores SET Activo = 0 WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", proveedorId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarEliminacion(usuario, "Proveedores", proveedorId.ToString());
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar proveedor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un nuevo proveedor
        /// </summary>
        public static int CrearProveedor(Proveedor proveedor, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"INSERT INTO Proveedores
                                   (Nombre, Contacto, Telefono, Email, Direccion, RUC, Activo, Notas)
                                   VALUES
                                   (@Nombre, @Contacto, @Telefono, @Email, @Direccion, @RUC, 1, @Notas);
                                   SELECT last_insert_rowid();";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", proveedor.Nombre);
                        cmd.Parameters.AddWithValue("@Contacto", proveedor.Contacto ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", proveedor.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Direccion", proveedor.Direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RUC", proveedor.RUC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notas", proveedor.Notas ?? (object)DBNull.Value);

                        int nuevoId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Registrar en auditoría
                        if (usuario != null)
                        {
                            AuditLogger.RegistrarCreacion(usuario, "Proveedores", nuevoId.ToString(), new
                            {
                                proveedor.Nombre,
                                proveedor.RUC
                            });
                        }

                        return nuevoId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear proveedor: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Actualiza un proveedor existente
        /// </summary>
        public static bool ActualizarProveedor(Proveedor proveedor, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"UPDATE Proveedores SET
                                   Nombre = @Nombre,
                                   Contacto = @Contacto,
                                   Telefono = @Telefono,
                                   Email = @Email,
                                   Direccion = @Direccion,
                                   RUC = @RUC,
                                   Notas = @Notas
                                   WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", proveedor.Id);
                        cmd.Parameters.AddWithValue("@Nombre", proveedor.Nombre);
                        cmd.Parameters.AddWithValue("@Contacto", proveedor.Contacto ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefono", proveedor.Telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", proveedor.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Direccion", proveedor.Direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RUC", proveedor.RUC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Notas", proveedor.Notas ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarActualizacion(usuario, "Proveedores", proveedor.Id.ToString(), null, new
                    {
                        proveedor.Nombre
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar proveedor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea una nueva orden de compra
        /// </summary>
        public static int CrearOrdenCompra(OrdenCompra orden, List<DetalleOrdenCompra> detalles, Usuario usuario)
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
                            // Crear orden
                            string queryOrden = @"INSERT INTO OrdenesCompra
                                                (ProveedorId, FechaOrden, FechaEntrega, Total, Estado, UsuarioId, Notas)
                                                VALUES
                                                (@ProveedorId, @FechaOrden, @FechaEntrega, @Total, @Estado, @UsuarioId, @Notas);
                                                SELECT last_insert_rowid();";

                            int ordenId;
                            using (var cmd = new SQLiteCommand(queryOrden, connection))
                            {
                                cmd.Parameters.AddWithValue("@ProveedorId", orden.ProveedorId);
                                cmd.Parameters.AddWithValue("@FechaOrden", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@FechaEntrega", orden.FechaEntrega ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Total", orden.Total);
                                cmd.Parameters.AddWithValue("@Estado", "Pendiente");
                                cmd.Parameters.AddWithValue("@UsuarioId", usuario?.Id ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Notas", orden.Notas ?? (object)DBNull.Value);

                                ordenId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Agregar detalles
                            string queryDetalle = @"INSERT INTO DetalleOrdenCompra
                                                  (OrdenCompraId, ProductoCodigo, Cantidad, PrecioUnitario, Subtotal)
                                                  VALUES
                                                  (@OrdenCompraId, @ProductoCodigo, @Cantidad, @PrecioUnitario, @Subtotal)";

                            foreach (var detalle in detalles)
                            {
                                using (var cmd = new SQLiteCommand(queryDetalle, connection))
                                {
                                    cmd.Parameters.AddWithValue("@OrdenCompraId", ordenId);
                                    cmd.Parameters.AddWithValue("@ProductoCodigo", detalle.ProductoCodigo);
                                    cmd.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                    cmd.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                                    cmd.Parameters.AddWithValue("@Subtotal", detalle.Subtotal);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            // Registrar en auditoría
                            if (usuario != null)
                            {
                                AuditLogger.RegistrarCreacion(usuario, "OrdenesCompra", ordenId.ToString(), new
                                {
                                    orden.ProveedorId,
                                    orden.Total,
                                    CantidadProductos = detalles.Count
                                });
                            }

                            return ordenId;
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
                System.Diagnostics.Debug.WriteLine($"Error al crear orden de compra: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Marca una orden como recibida y actualiza el inventario
        /// </summary>
        public static bool RecibirOrdenCompra(int ordenId, Usuario usuario)
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
                            // Obtener detalles de la orden
                            string queryDetalles = @"SELECT ProductoCodigo, Cantidad
                                                   FROM DetalleOrdenCompra
                                                   WHERE OrdenCompraId = @OrdenId";

                            var detalles = new List<(string productoCodigo, int cantidad)>();

                            using (var cmd = new SQLiteCommand(queryDetalles, connection))
                            {
                                cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        detalles.Add((reader.GetString(0), reader.GetInt32(1)));
                                    }
                                }
                            }

                            // Actualizar stock de cada producto
                            string queryUpdateStock = @"UPDATE Productos SET
                                                      Stock = Stock + @Cantidad
                                                      WHERE Codigo = @ProductoCodigo";

                            foreach (var (productoCodigo, cantidad) in detalles)
                            {
                                using (var cmd = new SQLiteCommand(queryUpdateStock, connection))
                                {
                                    cmd.Parameters.AddWithValue("@ProductoCodigo", productoCodigo);
                                    cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // Actualizar estado de la orden
                            string queryUpdateOrden = @"UPDATE OrdenesCompra SET
                                                      Estado = 'Recibido',
                                                      FechaEntrega = @FechaEntrega
                                                      WHERE Id = @Id";

                            using (var cmd = new SQLiteCommand(queryUpdateOrden, connection))
                            {
                                cmd.Parameters.AddWithValue("@Id", ordenId);
                                cmd.Parameters.AddWithValue("@FechaEntrega", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            // Registrar en auditoría
                            if (usuario != null)
                            {
                                AuditLogger.RegistrarActualizacion(usuario, "OrdenesCompra", ordenId.ToString(), null, new
                                {
                                    Estado = "Recibido",
                                    ProductosActualizados = detalles.Count
                                });
                            }

                            return true;
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
                System.Diagnostics.Debug.WriteLine($"Error al recibir orden: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene las órdenes de compra
        /// </summary>
        public static List<OrdenCompra> ObtenerOrdenesCompra()
        {
            var ordenes = new List<OrdenCompra>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT oc.*, p.Nombre as ProveedorNombre
                                   FROM OrdenesCompra oc
                                   INNER JOIN Proveedores p ON oc.ProveedorId = p.Id
                                   ORDER BY oc.FechaOrden DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ordenes.Add(new OrdenCompra
                                {
                                    Id = reader.GetInt32(0),
                                    ProveedorId = reader.GetInt32(1),
                                    FechaOrden = reader.GetString(2),
                                    FechaEntrega = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Total = reader.GetDecimal(4),
                                    Estado = reader.GetString(5),
                                    UsuarioId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                                    Notas = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    ProveedorNombre = reader.GetString(8)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener órdenes: {ex.Message}");
            }

            return ordenes;
        }

        /// <summary>
        /// Obtiene los detalles de una orden
        /// </summary>
        public static List<DetalleOrdenCompra> ObtenerDetallesOrden(int ordenId)
        {
            var detalles = new List<DetalleOrdenCompra>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT doc.*, p.Nombre as ProductoNombre
                                   FROM DetalleOrdenCompra doc
                                   INNER JOIN Productos p ON doc.ProductoCodigo = p.Codigo
                                   WHERE doc.OrdenCompraId = @OrdenId";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@OrdenId", ordenId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                detalles.Add(new DetalleOrdenCompra
                                {
                                    Id = reader.GetInt32(0),
                                    OrdenCompraId = reader.GetInt32(1),
                                    ProductoCodigo = reader.GetString(2),
                                    Cantidad = reader.GetInt32(3),
                                    PrecioUnitario = reader.GetDecimal(4),
                                    Subtotal = reader.GetDecimal(5),
                                    ProductoNombre = reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener detalles de orden: {ex.Message}");
            }

            return detalles;
        }
    }

    /// <summary>
    /// Clase que representa un proveedor
    /// </summary>
    public class Proveedor
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Contacto { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string RUC { get; set; }
        public bool Activo { get; set; }
        public string Notas { get; set; }

        // Campos calculados
        public int TotalOrdenes { get; set; }
        public decimal TotalCompras { get; set; }

        public string TotalComprasFormateado => TotalCompras.ToString("C");
    }

    /// <summary>
    /// Clase que representa una orden de compra
    /// </summary>
    public class OrdenCompra
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public string FechaOrden { get; set; }
        public string FechaEntrega { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; }
        public int? UsuarioId { get; set; }
        public string Notas { get; set; }

        // Campos relacionados
        public string ProveedorNombre { get; set; }

        public string TotalFormateado => Total.ToString("C");
        public string EstadoFormateado => Estado switch
        {
            "Pendiente" => "⏱ Pendiente",
            "Recibido" => "✓ Recibido",
            "Cancelado" => "✗ Cancelado",
            _ => Estado
        };
    }

    /// <summary>
    /// Clase que representa el detalle de una orden
    /// </summary>
    public class DetalleOrdenCompra
    {
        public int Id { get; set; }
        public int OrdenCompraId { get; set; }
        public string ProductoCodigo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        // Campos relacionados
        public string ProductoNombre { get; set; }

        public string PrecioUnitarioFormateado => PrecioUnitario.ToString("C");
        public string SubtotalFormateado => Subtotal.ToString("C");
    }
}
