using System;
using System.Data.SQLite;
using System.Text.Json;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Helper para registrar todas las acciones de los usuarios en el sistema de auditoría
    /// </summary>
    public static class AuditLogger
    {
        /// <summary>
        /// Tipos de acciones que se pueden auditar
        /// </summary>
        public enum TipoAccion
        {
            // Autenticación
            Login,
            Logout,
            LoginFallido,

            // CRUD General
            Crear,
            Actualizar,
            Eliminar,
            Consultar,

            // Ventas
            VentaRealizada,
            VentaCancelada,
            VentaModificada,
            DevolucionRealizada,

            // Turnos y Caja
            TurnoAbierto,
            TurnoCerrado,
            MovimientoCaja,
            ArqueoCaja,

            // Productos
            ProductoCreado,
            ProductoEditado,
            ProductoEliminado,
            StockModificado,

            // Clientes
            ClienteCreado,
            ClienteEditado,
            ClienteEliminado,
            CreditoOtorgado,
            PagoCredito,

            // Proveedores
            ProveedorCreado,
            ProveedorEditado,
            ProveedorEliminado,
            OrdenCompraCreada,
            OrdenCompraRecibida,
            OrdenCompraCancelada,

            // Usuarios
            UsuarioCreado,
            UsuarioEditado,
            UsuarioEliminado,
            CambioPassword,
            CambioRol,

            // Sistema
            ConfiguracionCambiada,
            BackupCreado,
            BackupRestaurado,
            PermisosDenegados,
            ErrorSistema,

            // Reportes
            ReporteGenerado,
            ExportacionDatos
        }

        /// <summary>
        /// Registra una acción en el log de auditoría
        /// </summary>
        /// <param name="usuarioId">ID del usuario que realiza la acción</param>
        /// <param name="usuarioNombre">Nombre del usuario</param>
        /// <param name="accion">Tipo de acción</param>
        /// <param name="tabla">Tabla afectada (opcional)</param>
        /// <param name="registroId">ID del registro afectado (opcional)</param>
        /// <param name="datosAnteriores">Datos antes del cambio en JSON (opcional)</param>
        /// <param name="datosNuevos">Datos después del cambio en JSON (opcional)</param>
        /// <param name="detalles">Detalles adicionales (opcional)</param>
        public static void RegistrarAccion(
            int? usuarioId,
            string usuarioNombre,
            TipoAccion accion,
            string? tabla = null,
            string? registroId = null,
            object? datosAnteriores = null,
            object? datosNuevos = null,
            string? detalles = null)
        {
            try
            {
                string query = @"INSERT INTO AuditLog
                    (Fecha, UsuarioId, UsuarioNombre, Accion, Tabla, RegistroId, DatosAnteriores, DatosNuevos, Detalles)
                    VALUES
                    (@Fecha, @UsuarioId, @UsuarioNombre, @Accion, @Tabla, @RegistroId, @DatosAnteriores, @DatosNuevos, @Detalles)";

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@UsuarioId", usuarioId.HasValue ? (object)usuarioId.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@UsuarioNombre", usuarioNombre ?? "Sistema");
                        cmd.Parameters.AddWithValue("@Accion", accion.ToString());
                        cmd.Parameters.AddWithValue("@Tabla", tabla ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RegistroId", registroId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DatosAnteriores",
                            datosAnteriores != null ? JsonSerializer.Serialize(datosAnteriores) : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DatosNuevos",
                            datosNuevos != null ? JsonSerializer.Serialize(datosNuevos) : (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Detalles", detalles ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // No lanzar excepción para no interrumpir la operación principal
                // pero registrar en consola para debugging
                System.Diagnostics.Debug.WriteLine($"Error al registrar auditoría: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra un login exitoso
        /// </summary>
        public static void RegistrarLogin(Usuario usuario)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.Login,
                detalles: $"Login exitoso desde IP: {ObtenerIPLocal()}"
            );
        }

        /// <summary>
        /// Registra un logout
        /// </summary>
        public static void RegistrarLogout(Usuario usuario)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.Logout,
                detalles: "Sesión cerrada correctamente"
            );
        }

        /// <summary>
        /// Registra una venta realizada
        /// </summary>
        public static void RegistrarVenta(Usuario usuario, int ventaId, decimal total, int cantidadProductos)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.VentaRealizada,
                tabla: "Ventas",
                registroId: ventaId.ToString(),
                detalles: $"Venta por {total:C} con {cantidadProductos} productos"
            );
        }

        /// <summary>
        /// Registra apertura de turno
        /// </summary>
        public static void RegistrarAperturaTurno(Usuario usuario, int turnoId, decimal montoInicial)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.TurnoAbierto,
                tabla: "Turnos",
                registroId: turnoId.ToString(),
                detalles: $"Turno abierto con monto inicial: {montoInicial:C}"
            );
        }

        /// <summary>
        /// Registra cierre de turno
        /// </summary>
        public static void RegistrarCierreTurno(Usuario usuario, int turnoId, decimal totalEfectivo, decimal diferencia)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.TurnoCerrado,
                tabla: "Turnos",
                registroId: turnoId.ToString(),
                detalles: $"Turno cerrado. Total: {totalEfectivo:C}, Diferencia: {diferencia:C}"
            );
        }

        /// <summary>
        /// Registra un movimiento de caja
        /// </summary>
        public static void RegistrarMovimientoCaja(Usuario usuario, string tipo, decimal monto, string concepto)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.MovimientoCaja,
                tabla: "MovimientosCaja",
                detalles: $"{tipo}: {monto:C} - {concepto}"
            );
        }

        /// <summary>
        /// Registra creación de un registro
        /// </summary>
        public static void RegistrarCreacion(Usuario usuario, string tabla, string registroId, object datosNuevos)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.Crear,
                tabla: tabla,
                registroId: registroId,
                datosNuevos: datosNuevos,
                detalles: $"Nuevo registro creado en {tabla}"
            );
        }

        /// <summary>
        /// Registra actualización de un registro
        /// </summary>
        public static void RegistrarActualizacion(Usuario usuario, string tabla, string registroId,
            object datosAnteriores, object datosNuevos)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.Actualizar,
                tabla: tabla,
                registroId: registroId,
                datosAnteriores: datosAnteriores,
                datosNuevos: datosNuevos,
                detalles: $"Registro actualizado en {tabla}"
            );
        }

        /// <summary>
        /// Registra eliminación de un registro
        /// </summary>
        public static void RegistrarEliminacion(Usuario usuario, string tabla, string registroId, object datosAnteriores)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.Eliminar,
                tabla: tabla,
                registroId: registroId,
                datosAnteriores: datosAnteriores,
                detalles: $"Registro eliminado de {tabla}"
            );
        }

        /// <summary>
        /// Registra cambio de contraseña
        /// </summary>
        public static void RegistrarCambioPassword(Usuario usuario, int usuarioAfectadoId, string nombreAfectado)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.CambioPassword,
                tabla: "Usuarios",
                registroId: usuarioAfectadoId.ToString(),
                detalles: $"Contraseña cambiada para usuario: {nombreAfectado}"
            );
        }

        /// <summary>
        /// Registra cambio de configuración
        /// </summary>
        public static void RegistrarCambioConfiguracion(Usuario usuario, string campo, object valorAnterior, object valorNuevo)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.ConfiguracionCambiada,
                tabla: "Configuracion",
                datosAnteriores: new { campo, valor = valorAnterior },
                datosNuevos: new { campo, valor = valorNuevo },
                detalles: $"Configuración '{campo}' cambiada"
            );
        }

        /// <summary>
        /// Registra creación de backup
        /// </summary>
        public static void RegistrarBackup(Usuario usuario, string rutaArchivo, long tamanoBytes)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.BackupCreado,
                detalles: $"Backup creado: {rutaArchivo} ({tamanoBytes / 1024.0:N2} KB)"
            );
        }

        /// <summary>
        /// Registra restauración de backup
        /// </summary>
        public static void RegistrarRestauracion(Usuario usuario, string rutaArchivo)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.BackupRestaurado,
                detalles: $"Base de datos restaurada desde: {rutaArchivo}"
            );
        }

        /// <summary>
        /// Registra intento de acción sin permisos
        /// </summary>
        public static void RegistrarPermisosDenegados(Usuario usuario, string accionIntentada)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.PermisosDenegados,
                detalles: $"Acceso denegado a: {accionIntentada}"
            );
        }

        /// <summary>
        /// Registra cancelación de una venta
        /// </summary>
        public static void RegistrarCancelacionVenta(Usuario usuario, int ventaId, decimal total, string motivo, object datosVenta)
        {
            RegistrarAccion(
                usuarioId: usuario.IdUsuario,
                usuarioNombre: usuario.Nombre,
                accion: TipoAccion.VentaCancelada,
                tabla: "Ventas",
                registroId: ventaId.ToString(),
                datosAnteriores: datosVenta,
                detalles: $"Venta #{ventaId} cancelada por {total:C}. Motivo: {motivo}"
            );
        }

        /// <summary>
        /// Registra un intento de login fallido
        /// </summary>
        public static void RegistrarLoginFallido(string nombreUsuario)
        {
            RegistrarAccion(
                usuarioId: null,
                usuarioNombre: nombreUsuario,
                accion: TipoAccion.LoginFallido,
                detalles: $"Intento de login fallido desde IP: {ObtenerIPLocal()}"
            );
        }

        /// <summary>
        /// Registra la creación de un producto
        /// </summary>
        public static void RegistrarProductoCreado(Usuario usuario, string codigo, string nombre, decimal precio, int stock)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ProductoCreado,
                tabla: "Productos",
                registroId: codigo,
                datosNuevos: new { codigo, nombre, precio, stock },
                detalles: $"Producto '{nombre}' ({codigo}) creado con precio {precio:C} y stock {stock}"
            );
        }

        /// <summary>
        /// Registra la edición de un producto
        /// </summary>
        public static void RegistrarProductoEditado(Usuario usuario, string codigo, object datosAnteriores, object datosNuevos)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ProductoEditado,
                tabla: "Productos",
                registroId: codigo,
                datosAnteriores: datosAnteriores,
                datosNuevos: datosNuevos,
                detalles: $"Producto {codigo} modificado"
            );
        }

        /// <summary>
        /// Registra la eliminación de un producto
        /// </summary>
        public static void RegistrarProductoEliminado(Usuario usuario, string codigo, string nombre, object datosProducto)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ProductoEliminado,
                tabla: "Productos",
                registroId: codigo,
                datosAnteriores: datosProducto,
                detalles: $"Producto '{nombre}' ({codigo}) eliminado"
            );
        }

        /// <summary>
        /// Registra modificación de stock
        /// </summary>
        public static void RegistrarModificacionStock(Usuario usuario, string codigo, int stockAnterior, int stockNuevo, string razon)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.StockModificado,
                tabla: "Productos",
                registroId: codigo,
                datosAnteriores: new { stock = stockAnterior },
                datosNuevos: new { stock = stockNuevo },
                detalles: $"Stock de {codigo} cambió de {stockAnterior} a {stockNuevo}. Razón: {razon}"
            );
        }

        /// <summary>
        /// Registra creación de usuario
        /// </summary>
        public static void RegistrarUsuarioCreado(Usuario adminUsuario, int nuevoUsuarioId, string nombreNuevo, string rolNuevo)
        {
            RegistrarAccion(
                usuarioId: adminUsuario.IdUsuario,
                usuarioNombre: adminUsuario.Nombre,
                accion: TipoAccion.UsuarioCreado,
                tabla: "Usuarios",
                registroId: nuevoUsuarioId.ToString(),
                datosNuevos: new { nombre = nombreNuevo, rol = rolNuevo },
                detalles: $"Usuario '{nombreNuevo}' creado con rol '{rolNuevo}'"
            );
        }

        /// <summary>
        /// Registra eliminación de usuario
        /// </summary>
        public static void RegistrarUsuarioEliminado(Usuario adminUsuario, int usuarioEliminadoId, string nombreEliminado)
        {
            RegistrarAccion(
                usuarioId: adminUsuario.IdUsuario,
                usuarioNombre: adminUsuario.Nombre,
                accion: TipoAccion.UsuarioEliminado,
                tabla: "Usuarios",
                registroId: usuarioEliminadoId.ToString(),
                detalles: $"Usuario '{nombreEliminado}' eliminado del sistema"
            );
        }

        /// <summary>
        /// Registra cambio de rol de usuario
        /// </summary>
        public static void RegistrarCambioRol(Usuario adminUsuario, int usuarioAfectadoId, string nombreAfectado, string rolAnterior, string rolNuevo)
        {
            RegistrarAccion(
                usuarioId: adminUsuario.IdUsuario,
                usuarioNombre: adminUsuario.Nombre,
                accion: TipoAccion.CambioRol,
                tabla: "Usuarios",
                registroId: usuarioAfectadoId.ToString(),
                datosAnteriores: new { rol = rolAnterior },
                datosNuevos: new { rol = rolNuevo },
                detalles: $"Rol de '{nombreAfectado}' cambiado de '{rolAnterior}' a '{rolNuevo}'"
            );
        }

        /// <summary>
        /// Registra generación de reporte
        /// </summary>
        public static void RegistrarReporteGenerado(Usuario usuario, string tipoReporte, string rutaArchivo)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ReporteGenerado,
                detalles: $"Reporte '{tipoReporte}' generado: {rutaArchivo}"
            );
        }

        /// <summary>
        /// Registra exportación de datos
        /// </summary>
        public static void RegistrarExportacion(Usuario usuario, string tipoExportacion, string rutaArchivo, int cantidadRegistros)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ExportacionDatos,
                detalles: $"Exportación {tipoExportacion}: {cantidadRegistros} registros a {rutaArchivo}"
            );
        }

        /// <summary>
        /// Registra error del sistema
        /// </summary>
        public static void RegistrarError(Usuario usuario, string operacion, string mensajeError)
        {
            RegistrarAccion(
                usuarioId: usuario?.IdUsuario,
                usuarioNombre: usuario?.Nombre ?? "Sistema",
                accion: TipoAccion.ErrorSistema,
                detalles: $"Error en '{operacion}': {mensajeError}"
            );
        }

        /// <summary>
        /// Obtiene la IP local de la máquina (para registro de login)
        /// </summary>
        private static string ObtenerIPLocal()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "localhost";
            }
            catch
            {
                return "localhost";
            }
        }
    }
}
