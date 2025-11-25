using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Helper para gestionar clientes y CRM
    /// </summary>
    public static class ClientesHelper
    {
        /// <summary>
        /// Obtiene todos los clientes activos
        /// </summary>
        public static List<Cliente> ObtenerClientesActivos()
        {
            var clientes = new List<Cliente>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT c.*,
                                   COUNT(DISTINCT v.Id) as TotalCompras,
                                   COALESCE(SUM(v.Total), 0) as TotalGastado,
                                   COALESCE(SUM(cc.MontoPendiente), 0) as DeudaTotal
                                   FROM Clientes c
                                   LEFT JOIN Ventas v ON v.ClienteId = c.Id
                                   LEFT JOIN CreditoClientes cc ON cc.ClienteId = c.Id AND cc.Estado = 'Pendiente'
                                   WHERE c.Activo = 1
                                   GROUP BY c.Id
                                   ORDER BY c.Nombre";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                clientes.Add(new Cliente
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Apellido = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Documento = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Telefono = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Direccion = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    FechaNacimiento = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    LimiteCredito = reader.GetDouble(8),
                                    DescuentoEspecial = reader.GetDouble(9),
                                    FechaRegistro = reader.GetString(10),
                                    Activo = reader.GetInt32(11) == 1,
                                    Notas = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    TotalCompras = reader.GetInt32(13),
                                    TotalGastado = reader.GetDecimal(14),
                                    DeudaTotal = reader.GetDecimal(15)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener clientes: {ex.Message}");
            }

            return clientes;
        }

        /// <summary>
        /// Busca clientes por nombre, documento o teléfono
        /// </summary>
        public static List<Cliente> BuscarClientes(string busqueda)
        {
            var clientes = new List<Cliente>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT c.*,
                                   COUNT(DISTINCT v.Id) as TotalCompras,
                                   COALESCE(SUM(v.Total), 0) as TotalGastado,
                                   COALESCE(SUM(cc.MontoPendiente), 0) as DeudaTotal
                                   FROM Clientes c
                                   LEFT JOIN Ventas v ON v.ClienteId = c.Id
                                   LEFT JOIN CreditoClientes cc ON cc.ClienteId = c.Id AND cc.Estado = 'Pendiente'
                                   WHERE c.Activo = 1
                                   AND (c.Nombre LIKE @Busqueda OR c.Apellido LIKE @Busqueda
                                        OR c.Documento LIKE @Busqueda OR c.Telefono LIKE @Busqueda)
                                   GROUP BY c.Id
                                   ORDER BY c.Nombre";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Busqueda", $"%{busqueda}%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                clientes.Add(new Cliente
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Apellido = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Documento = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Telefono = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Direccion = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    FechaNacimiento = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    LimiteCredito = reader.GetDouble(8),
                                    DescuentoEspecial = reader.GetDouble(9),
                                    FechaRegistro = reader.GetString(10),
                                    Activo = reader.GetInt32(11) == 1,
                                    Notas = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    TotalCompras = reader.GetInt32(13),
                                    TotalGastado = reader.GetDecimal(14),
                                    DeudaTotal = reader.GetDecimal(15)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al buscar clientes: {ex.Message}");
            }

            return clientes;
        }

        /// <summary>
        /// Obtiene un cliente por ID
        /// </summary>
        public static Cliente ObtenerClientePorId(int clienteId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT c.*,
                                   COUNT(DISTINCT v.Id) as TotalCompras,
                                   COALESCE(SUM(v.Total), 0) as TotalGastado,
                                   COALESCE(SUM(cc.MontoPendiente), 0) as DeudaTotal
                                   FROM Clientes c
                                   LEFT JOIN Ventas v ON v.ClienteId = c.Id
                                   LEFT JOIN CreditoClientes cc ON cc.ClienteId = c.Id AND cc.Estado = 'Pendiente'
                                   WHERE c.Id = @Id
                                   GROUP BY c.Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", clienteId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Cliente
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Apellido = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Documento = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Telefono = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Direccion = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    FechaNacimiento = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    LimiteCredito = reader.GetDouble(8),
                                    DescuentoEspecial = reader.GetDouble(9),
                                    FechaRegistro = reader.GetString(10),
                                    Activo = reader.GetInt32(11) == 1,
                                    Notas = reader.IsDBNull(12) ? null : reader.GetString(12),
                                    TotalCompras = reader.GetInt32(13),
                                    TotalGastado = reader.GetDecimal(14),
                                    DeudaTotal = reader.GetDecimal(15)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener cliente: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Crea un nuevo cliente
        /// </summary>
        public static int CrearCliente(Cliente cliente, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"INSERT INTO Clientes
                                   (Nombre, Apellido, Documento, Telefono, Email, Direccion, FechaNacimiento,
                                    LimiteCredito, DescuentoEspecial, FechaRegistro, Activo, Notas)
                                   VALUES
                                   (@Nombre, @Apellido, @Documento, @Telefono, @Email, @Direccion, @FechaNacimiento,
                                    @LimiteCredito, @DescuentoEspecial, @FechaRegistro, @Activo, @Notas);
                                   SELECT last_insert_rowid();";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", cliente.Nombre);
                        cmd.Parameters.AddWithValue("@Apellido", cliente.Apellido ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Documento", cliente.Documento ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefono", cliente.Telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", cliente.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Direccion", cliente.Direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", cliente.FechaNacimiento ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LimiteCredito", cliente.LimiteCredito);
                        cmd.Parameters.AddWithValue("@DescuentoEspecial", cliente.DescuentoEspecial);
                        cmd.Parameters.AddWithValue("@FechaRegistro", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@Activo", 1);
                        cmd.Parameters.AddWithValue("@Notas", cliente.Notas ?? (object)DBNull.Value);

                        int nuevoId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Registrar en auditoría
                        if (usuario != null)
                        {
                            AuditLogger.RegistrarCreacion(usuario, "Clientes", nuevoId.ToString(), new
                            {
                                cliente.Nombre,
                                cliente.Documento
                            });
                        }

                        return nuevoId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear cliente: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Actualiza un cliente existente
        /// </summary>
        public static bool ActualizarCliente(Cliente cliente, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"UPDATE Clientes SET
                                   Nombre = @Nombre,
                                   Apellido = @Apellido,
                                   Documento = @Documento,
                                   Telefono = @Telefono,
                                   Email = @Email,
                                   Direccion = @Direccion,
                                   FechaNacimiento = @FechaNacimiento,
                                   LimiteCredito = @LimiteCredito,
                                   DescuentoEspecial = @DescuentoEspecial,
                                   Notas = @Notas
                                   WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", cliente.Id);
                        cmd.Parameters.AddWithValue("@Nombre", cliente.Nombre);
                        cmd.Parameters.AddWithValue("@Apellido", cliente.Apellido ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Documento", cliente.Documento ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefono", cliente.Telefono ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", cliente.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Direccion", cliente.Direccion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaNacimiento", cliente.FechaNacimiento ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LimiteCredito", cliente.LimiteCredito);
                        cmd.Parameters.AddWithValue("@DescuentoEspecial", cliente.DescuentoEspecial);
                        cmd.Parameters.AddWithValue("@Notas", cliente.Notas ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarActualizacion(usuario, "Clientes", cliente.Id.ToString(), null, new
                    {
                        cliente.Nombre,
                        cliente.LimiteCredito
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar cliente: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Elimina (desactiva) un cliente
        /// </summary>
        public static bool EliminarCliente(int clienteId, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE Clientes SET Activo = 0 WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", clienteId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarEliminacion(usuario, "Clientes", clienteId.ToString(), null);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar cliente: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si un cliente tiene crédito disponible
        /// </summary>
        public static bool TieneCreditoDisponible(int clienteId, decimal monto)
        {
            var cliente = ObtenerClientePorId(clienteId);
            if (cliente == null) return false;

            decimal creditoDisponible = (decimal)cliente.LimiteCredito - cliente.DeudaTotal;
            return creditoDisponible >= monto;
        }

        /// <summary>
        /// Registra una venta a crédito
        /// </summary>
        public static bool RegistrarVentaCredito(int clienteId, int ventaId, decimal montoTotal, int diasVencimiento = 30)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"INSERT INTO CreditoClientes
                                   (ClienteId, VentaId, MontoTotal, MontoAbonado, MontoPendiente, FechaVenta, FechaVencimiento, Estado)
                                   VALUES
                                   (@ClienteId, @VentaId, @MontoTotal, 0, @MontoTotal, @FechaVenta, @FechaVencimiento, 'Pendiente')";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                        cmd.Parameters.AddWithValue("@VentaId", ventaId);
                        cmd.Parameters.AddWithValue("@MontoTotal", montoTotal);
                        cmd.Parameters.AddWithValue("@FechaVenta", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@FechaVencimiento", DateTime.Now.AddDays(diasVencimiento).ToString("yyyy-MM-dd"));

                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al registrar venta a crédito: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registra un pago de crédito
        /// </summary>
        public static bool RegistrarPagoCredito(int creditoId, decimal monto, string metodoPago, Usuario usuario, string notas = null)
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
                            // Registrar el pago
                            string queryPago = @"INSERT INTO PagosCredito
                                               (CreditoId, Monto, FechaPago, MetodoPago, UsuarioId, Notas)
                                               VALUES
                                               (@CreditoId, @Monto, @FechaPago, @MetodoPago, @UsuarioId, @Notas)";

                            using (var cmd = new SQLiteCommand(queryPago, connection))
                            {
                                cmd.Parameters.AddWithValue("@CreditoId", creditoId);
                                cmd.Parameters.AddWithValue("@Monto", monto);
                                cmd.Parameters.AddWithValue("@FechaPago", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@MetodoPago", metodoPago);
                                cmd.Parameters.AddWithValue("@UsuarioId", usuario?.IdUsuario ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Notas", notas ?? (object)DBNull.Value);

                                cmd.ExecuteNonQuery();
                            }

                            // Actualizar el crédito
                            string queryUpdate = @"UPDATE CreditoClientes SET
                                                 MontoAbonado = MontoAbonado + @Monto,
                                                 MontoPendiente = MontoPendiente - @Monto,
                                                 Estado = CASE WHEN (MontoPendiente - @Monto) <= 0 THEN 'Pagado' ELSE 'Pendiente' END
                                                 WHERE Id = @Id";

                            using (var cmd = new SQLiteCommand(queryUpdate, connection))
                            {
                                cmd.Parameters.AddWithValue("@Id", creditoId);
                                cmd.Parameters.AddWithValue("@Monto", monto);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            // Registrar en auditoría
                            if (usuario != null)
                            {
                                AuditLogger.RegistrarCreacion(usuario, "PagosCredito", null, new
                                {
                                    creditoId,
                                    monto,
                                    metodoPago
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
                System.Diagnostics.Debug.WriteLine($"Error al registrar pago: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene los créditos pendientes de un cliente
        /// </summary>
        public static List<CreditoCliente> ObtenerCreditosCliente(int clienteId)
        {
            var creditos = new List<CreditoCliente>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT * FROM CreditoClientes
                                   WHERE ClienteId = @ClienteId
                                   ORDER BY FechaVenta DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@ClienteId", clienteId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                creditos.Add(new CreditoCliente
                                {
                                    Id = reader.GetInt32(0),
                                    ClienteId = reader.GetInt32(1),
                                    VentaId = reader.GetInt32(2),
                                    MontoTotal = reader.GetDecimal(3),
                                    MontoAbonado = reader.GetDecimal(4),
                                    MontoPendiente = reader.GetDecimal(5),
                                    FechaVenta = reader.GetString(6),
                                    FechaVencimiento = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    Estado = reader.GetString(8)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener créditos: {ex.Message}");
            }

            return creditos;
        }

        /// <summary>
        /// Obtiene un crédito por su ID
        /// </summary>
        public static CreditoCliente ObtenerCreditoPorId(int creditoId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT * FROM CreditoClientes WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", creditoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new CreditoCliente
                                {
                                    Id = reader.GetInt32(0),
                                    ClienteId = reader.GetInt32(1),
                                    VentaId = reader.GetInt32(2),
                                    MontoTotal = reader.GetDecimal(3),
                                    MontoAbonado = reader.GetDecimal(4),
                                    MontoPendiente = reader.GetDecimal(5),
                                    FechaVenta = reader.GetString(6),
                                    FechaVencimiento = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    Estado = reader.GetString(8)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener crédito: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Obtiene los pagos realizados para un crédito específico
        /// </summary>
        public static List<PagoCredito> ObtenerPagosCliente(int creditoId)
        {
            var pagos = new List<PagoCredito>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT * FROM PagosCredito
                                   WHERE CreditoId = @CreditoId
                                   ORDER BY FechaPago DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@CreditoId", creditoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pagos.Add(new PagoCredito
                                {
                                    Id = reader.GetInt32(0),
                                    CreditoId = reader.GetInt32(1),
                                    Monto = reader.GetDecimal(2),
                                    FechaPago = reader.GetString(3),
                                    MetodoPago = reader.GetString(4),
                                    Notas = reader.IsDBNull(6) ? null : reader.GetString(6)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener pagos: {ex.Message}");
            }

            return pagos;
        }
    }

    /// <summary>
    /// Clase que representa un cliente
    /// </summary>
    public class Cliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Documento { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Direccion { get; set; }
        public string FechaNacimiento { get; set; }
        public double LimiteCredito { get; set; }
        public double DescuentoEspecial { get; set; }
        public string FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public string Notas { get; set; }

        // Campos calculados
        public int TotalCompras { get; set; }
        public decimal TotalGastado { get; set; }
        public decimal DeudaTotal { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        public string TotalGastadoFormateado => TotalGastado.ToString("C");
        public string DeudaTotalFormateada => DeudaTotal.ToString("C");
        public string LimiteCreditoFormateado => LimiteCredito.ToString("C");
        public string DescuentoFormateado => $"{DescuentoEspecial}%";
        public decimal CreditoDisponible => (decimal)LimiteCredito - DeudaTotal;
        public string CreditoDisponibleFormateado => CreditoDisponible.ToString("C");
    }

    /// <summary>
    /// Clase que representa un crédito de cliente
    /// </summary>
    public class CreditoCliente
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int VentaId { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal MontoAbonado { get; set; }
        public decimal MontoPendiente { get; set; }
        public string FechaVenta { get; set; }
        public string FechaVencimiento { get; set; }
        public string Estado { get; set; }

        public string MontoTotalFormateado => MontoTotal.ToString("C");
        public string MontoAbonadoFormateado => MontoAbonado.ToString("C");
        public string MontoPendienteFormateado => MontoPendiente.ToString("C");
        public string EstadoFormateado => Estado == "Pagado" ? "✓ Pagado" : "⏱ Pendiente";
        public string FechaVentaFormateada
        {
            get
            {
                if (DateTime.TryParse(FechaVenta, out DateTime fecha))
                    return fecha.ToString("dd/MM/yyyy");
                return FechaVenta;
            }
        }
        public string FechaVencimientoFormateada
        {
            get
            {
                if (string.IsNullOrEmpty(FechaVencimiento))
                    return "Sin vencimiento";
                if (DateTime.TryParse(FechaVencimiento, out DateTime fecha))
                    return fecha.ToString("dd/MM/yyyy");
                return FechaVencimiento;
            }
        }
    }

    /// <summary>
    /// Clase que representa un pago de crédito
    /// </summary>
    public class PagoCredito
    {
        public int Id { get; set; }
        public int CreditoId { get; set; }
        public decimal Monto { get; set; }
        public string FechaPago { get; set; }
        public string MetodoPago { get; set; }
        public string Notas { get; set; }

        public string MontoFormateado => Monto.ToString("C");
        public string FechaPagoFormateada
        {
            get
            {
                if (DateTime.TryParse(FechaPago, out DateTime fecha))
                    return fecha.ToString("dd/MM/yyyy HH:mm");
                return FechaPago;
            }
        }
    }
}
