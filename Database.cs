using System;
using System.Data.SQLite;
using System.IO;

namespace TuTiendita
{
    public static class Database
    {
        private static string dbPath = "Data Source=productos.db";

        static Database()
        {
            bool isNewDatabase = !File.Exists("./productos.db");

            if (isNewDatabase)
            {
                SQLiteConnection.CreateFile("productos.db");
            }

            // Always ensure tables exist (for both new and existing databases)
            using (var connection = new SQLiteConnection(dbPath))
            {
                connection.Open();

                    // Create Productos table
                    string createProductosQuery = @"CREATE TABLE IF NOT EXISTS [Productos] (
                                                [Codigo] TEXT NOT NULL PRIMARY KEY,
                                                [Nombre] TEXT NOT NULL,
                                                [Precio] REAL NOT NULL,
                                                [Stock] INTEGER NOT NULL
                                                )";
                    using (var cmd = new SQLiteCommand(createProductosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Usuarios table
                    string createUsuariosQuery = @"CREATE TABLE IF NOT EXISTS [Usuarios] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Nombre] TEXT NOT NULL,
                                                [Contrasena] TEXT NOT NULL,
                                                [NivelAcceso] TEXT NOT NULL
                                                )";
                    using (var cmd = new SQLiteCommand(createUsuariosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Turnos (Shifts) table
                    string createTurnosQuery = @"CREATE TABLE IF NOT EXISTS [Turnos] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [UsuarioId] INTEGER NOT NULL,
                                                [UsuarioNombre] TEXT NOT NULL,
                                                [FechaApertura] TEXT NOT NULL,
                                                [FechaCierre] TEXT,
                                                [MontoInicial] REAL NOT NULL,
                                                [MontoFinal] REAL,
                                                [TotalVentas] REAL DEFAULT 0,
                                                [Estado] TEXT NOT NULL,
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createTurnosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Ventas (Sales) table
                    string createVentasQuery = @"CREATE TABLE IF NOT EXISTS [Ventas] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [TurnoId] INTEGER,
                                                [UsuarioId] INTEGER NOT NULL,
                                                [UsuarioNombre] TEXT NOT NULL,
                                                [Fecha] TEXT NOT NULL,
                                                [Total] REAL NOT NULL,
                                                [MontoPagado] REAL NOT NULL,
                                                [Cambio] REAL NOT NULL,
                                                FOREIGN KEY ([TurnoId]) REFERENCES [Turnos]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createVentasQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create DetalleVentas (Sales Details) table
                    string createDetalleVentasQuery = @"CREATE TABLE IF NOT EXISTS [DetalleVentas] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [VentaId] INTEGER NOT NULL,
                                                [ProductoCodigo] TEXT NOT NULL,
                                                [ProductoNombre] TEXT NOT NULL,
                                                [Cantidad] INTEGER NOT NULL,
                                                [PrecioUnitario] REAL NOT NULL,
                                                [Subtotal] REAL NOT NULL,
                                                FOREIGN KEY ([VentaId]) REFERENCES [Ventas]([Id]),
                                                FOREIGN KEY ([ProductoCodigo]) REFERENCES [Productos]([Codigo])
                                                )";
                    using (var cmd = new SQLiteCommand(createDetalleVentasQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create AuditLog table for tracking ALL user actions
                    string createAuditLogQuery = @"CREATE TABLE IF NOT EXISTS [AuditLog] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Fecha] TEXT NOT NULL,
                                                [UsuarioId] INTEGER,
                                                [UsuarioNombre] TEXT NOT NULL,
                                                [Accion] TEXT NOT NULL,
                                                [Tabla] TEXT,
                                                [RegistroId] TEXT,
                                                [DatosAnteriores] TEXT,
                                                [DatosNuevos] TEXT,
                                                [Detalles] TEXT,
                                                [DireccionIP] TEXT
                                                )";
                    using (var cmd = new SQLiteCommand(createAuditLogQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Add Estado column to Ventas table if not exists (for cancellations)
                    try
                    {
                        string addEstadoColumn = "ALTER TABLE [Ventas] ADD COLUMN [Estado] TEXT DEFAULT 'Completada'";
                        using (var cmd = new SQLiteCommand(addEstadoColumn, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Add MotivoCancelacion column to Ventas table if not exists
                    try
                    {
                        string addMotivoColumn = "ALTER TABLE [Ventas] ADD COLUMN [MotivoCancelacion] TEXT";
                        using (var cmd = new SQLiteCommand(addMotivoColumn, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Add CanceladoPor column to Ventas table if not exists
                    try
                    {
                        string addCanceladoPorColumn = "ALTER TABLE [Ventas] ADD COLUMN [CanceladoPor] TEXT";
                        using (var cmd = new SQLiteCommand(addCanceladoPorColumn, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Add FechaCancelacion column to Ventas table if not exists
                    try
                    {
                        string addFechaCancelacionColumn = "ALTER TABLE [Ventas] ADD COLUMN [FechaCancelacion] TEXT";
                        using (var cmd = new SQLiteCommand(addFechaCancelacionColumn, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Add MetodoPago column to Ventas table if not exists
                    try
                    {
                        string addMetodoPagoColumn = "ALTER TABLE [Ventas] ADD COLUMN [MetodoPago] TEXT DEFAULT 'Efectivo'";
                        using (var cmd = new SQLiteCommand(addMetodoPagoColumn, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                // Create default admin user if database is new
                if (isNewDatabase)
                {
                    string insertAdminQuery = @"INSERT INTO [Usuarios] ([Nombre], [Contrasena], [NivelAcceso])
                                                VALUES (@nombre, @contrasena, @nivel)";
                    using (var cmd = new SQLiteCommand(insertAdminQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@nombre", "admin");
                        cmd.Parameters.AddWithValue("@contrasena", "admin123");
                        cmd.Parameters.AddWithValue("@nivel", "Gerente");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(dbPath);
        }
    }
}