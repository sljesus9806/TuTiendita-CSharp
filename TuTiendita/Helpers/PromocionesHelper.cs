using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Helper para gestionar descuentos y promociones
    /// </summary>
    public static class PromocionesHelper
    {
        public enum TipoDescuento
        {
            Porcentaje,      // Descuento por porcentaje (ej: 20%)
            MontoFijo,       // Descuento por monto fijo (ej: $10)
            DosXUno,         // 2x1
            TresXDos         // 3x2
        }

        /// <summary>
        /// Obtiene todas las promociones activas y vigentes
        /// </summary>
        public static List<Promocion> ObtenerPromocionesActivas()
        {
            var promociones = new List<Promocion>();

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string query = @"SELECT * FROM Promociones
                                   WHERE Activo = 1
                                   AND DATE(@Hoy) BETWEEN DATE(FechaInicio) AND DATE(FechaFin)";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Hoy", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                promociones.Add(new Promocion
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    TipoDescuento = reader.GetString(3),
                                    ValorDescuento = reader.GetDouble(4),
                                    ProductoId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    CodigoCupon = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    FechaInicio = reader.GetString(7),
                                    FechaFin = reader.GetString(8),
                                    Activo = reader.GetInt32(9) == 1,
                                    MontoMinimo = reader.GetDouble(10)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener promociones: {ex.Message}");
            }

            return promociones;
        }

        /// <summary>
        /// Valida un código de cupón
        /// </summary>
        public static Promocion ValidarCupon(string codigoCupon)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string query = @"SELECT * FROM Promociones
                                   WHERE CodigoCupon = @Codigo
                                   AND Activo = 1
                                   AND DATE(@Hoy) BETWEEN DATE(FechaInicio) AND DATE(FechaFin)";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Codigo", codigoCupon);
                        cmd.Parameters.AddWithValue("@Hoy", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Promocion
                                {
                                    Id = reader.GetInt32(0),
                                    Nombre = reader.GetString(1),
                                    Descripcion = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    TipoDescuento = reader.GetString(3),
                                    ValorDescuento = reader.GetDouble(4),
                                    ProductoId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                                    CodigoCupon = reader.GetString(6),
                                    FechaInicio = reader.GetString(7),
                                    FechaFin = reader.GetString(8),
                                    Activo = reader.GetInt32(9) == 1,
                                    MontoMinimo = reader.GetDouble(10)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al validar cupón: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Calcula el descuento para una venta
        /// </summary>
        public static (decimal descuento, string detalles) CalcularDescuento(
            List<Producto> productos,
            decimal subtotal,
            Promocion promocion)
        {
            if (promocion == null)
                return (0, "Sin descuento");

            // Validar monto mínimo
            if (subtotal < (decimal)promocion.MontoMinimo)
            {
                return (0, $"Monto mínimo requerido: {promocion.MontoMinimo:C}");
            }

            decimal descuento = 0;
            string detalles = "";

            switch (promocion.TipoDescuento.ToLower())
            {
                case "porcentaje":
                    descuento = subtotal * (decimal)(promocion.ValorDescuento / 100);
                    detalles = $"{promocion.Nombre} (-{promocion.ValorDescuento}%)";
                    break;

                case "montofijo":
                    descuento = (decimal)promocion.ValorDescuento;
                    detalles = $"{promocion.Nombre} (-{descuento:C})";
                    break;

                case "2x1":
                    if (promocion.ProductoId.HasValue)
                    {
                        var productosPromo = productos.Where(p => p.Id == promocion.ProductoId.Value).ToList();
                        int cantidadTotal = productosPromo.Sum(p => p.Cantidad);
                        int cantidadGratis = cantidadTotal / 2;

                        if (cantidadGratis > 0 && productosPromo.Any())
                        {
                            descuento = productosPromo.First().Precio * cantidadGratis;
                            detalles = $"{promocion.Nombre} (2x1: {cantidadGratis} gratis)";
                        }
                    }
                    break;

                case "3x2":
                    if (promocion.ProductoId.HasValue)
                    {
                        var productosPromo = productos.Where(p => p.Id == promocion.ProductoId.Value).ToList();
                        int cantidadTotal = productosPromo.Sum(p => p.Cantidad);
                        int cantidadGratis = cantidadTotal / 3;

                        if (cantidadGratis > 0 && productosPromo.Any())
                        {
                            descuento = productosPromo.First().Precio * cantidadGratis;
                            detalles = $"{promocion.Nombre} (3x2: {cantidadGratis} gratis)";
                        }
                    }
                    break;
            }

            return (descuento, detalles);
        }

        /// <summary>
        /// Obtiene promociones aplicables a un producto específico
        /// </summary>
        public static List<Promocion> ObtenerPromocionesProducto(int productoId)
        {
            return ObtenerPromocionesActivas()
                .Where(p => p.ProductoId == productoId || !p.ProductoId.HasValue)
                .ToList();
        }

        /// <summary>
        /// Crea una nueva promoción
        /// </summary>
        public static bool CrearPromocion(Promocion promocion, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"INSERT INTO Promociones
                                   (Nombre, Descripcion, TipoDescuento, ValorDescuento, ProductoId,
                                    CodigoCupon, FechaInicio, FechaFin, Activo, MontoMinimo)
                                   VALUES
                                   (@Nombre, @Descripcion, @TipoDescuento, @ValorDescuento, @ProductoId,
                                    @CodigoCupon, @FechaInicio, @FechaFin, @Activo, @MontoMinimo)";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", promocion.Nombre);
                        cmd.Parameters.AddWithValue("@Descripcion", promocion.Descripcion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TipoDescuento", promocion.TipoDescuento);
                        cmd.Parameters.AddWithValue("@ValorDescuento", promocion.ValorDescuento);
                        cmd.Parameters.AddWithValue("@ProductoId", promocion.ProductoId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CodigoCupon", promocion.CodigoCupon ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaInicio", promocion.FechaInicio);
                        cmd.Parameters.AddWithValue("@FechaFin", promocion.FechaFin);
                        cmd.Parameters.AddWithValue("@Activo", promocion.Activo ? 1 : 0);
                        cmd.Parameters.AddWithValue("@MontoMinimo", promocion.MontoMinimo);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarCreacion(usuario, "Promociones", null, new
                    {
                        promocion.Nombre,
                        promocion.TipoDescuento,
                        promocion.ValorDescuento
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear promoción: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Actualiza una promoción existente
        /// </summary>
        public static bool ActualizarPromocion(Promocion promocion, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"UPDATE Promociones SET
                                   Nombre = @Nombre,
                                   Descripcion = @Descripcion,
                                   TipoDescuento = @TipoDescuento,
                                   ValorDescuento = @ValorDescuento,
                                   ProductoId = @ProductoId,
                                   CodigoCupon = @CodigoCupon,
                                   FechaInicio = @FechaInicio,
                                   FechaFin = @FechaFin,
                                   Activo = @Activo,
                                   MontoMinimo = @MontoMinimo
                                   WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", promocion.Id);
                        cmd.Parameters.AddWithValue("@Nombre", promocion.Nombre);
                        cmd.Parameters.AddWithValue("@Descripcion", promocion.Descripcion ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TipoDescuento", promocion.TipoDescuento);
                        cmd.Parameters.AddWithValue("@ValorDescuento", promocion.ValorDescuento);
                        cmd.Parameters.AddWithValue("@ProductoId", promocion.ProductoId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CodigoCupon", promocion.CodigoCupon ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FechaInicio", promocion.FechaInicio);
                        cmd.Parameters.AddWithValue("@FechaFin", promocion.FechaFin);
                        cmd.Parameters.AddWithValue("@Activo", promocion.Activo ? 1 : 0);
                        cmd.Parameters.AddWithValue("@MontoMinimo", promocion.MontoMinimo);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarActualizacion(usuario, "Promociones", promocion.Id.ToString(), null, new
                    {
                        promocion.Nombre,
                        promocion.Activo
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar promoción: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Elimina (desactiva) una promoción
        /// </summary>
        public static bool EliminarPromocion(int promocionId, Usuario usuario)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE Promociones SET Activo = 0 WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", promocionId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Registrar en auditoría
                if (usuario != null)
                {
                    AuditLogger.RegistrarEliminacion(usuario, "Promociones", promocionId.ToString(), null);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar promoción: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Clase que representa una promoción
    /// </summary>
    public class Promocion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string TipoDescuento { get; set; }
        public double ValorDescuento { get; set; }
        public int? ProductoId { get; set; }
        public string CodigoCupon { get; set; }
        public string FechaInicio { get; set; }
        public string FechaFin { get; set; }
        public bool Activo { get; set; }
        public double MontoMinimo { get; set; }

        public string TipoDescuentoFormateado
        {
            get
            {
                return TipoDescuento?.ToLower() switch
                {
                    "porcentaje" => $"{ValorDescuento}% descuento",
                    "montofijo" => $"{ValorDescuento:C} descuento",
                    "2x1" => "2x1",
                    "3x2" => "3x2",
                    _ => TipoDescuento
                };
            }
        }

        public string EstadoFormateado => Activo ? "✓ Activa" : "✗ Inactiva";
    }
}
