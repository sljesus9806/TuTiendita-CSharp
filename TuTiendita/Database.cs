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

                // Enable foreign key constraints
                using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                    // Create Categorias table
                    string createCategoriasQuery = @"CREATE TABLE IF NOT EXISTS [Categorias] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Nombre] TEXT NOT NULL UNIQUE,
                                                [Descripcion] TEXT
                                                )";
                    using (var cmd = new SQLiteCommand(createCategoriasQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Productos table
                    string createProductosQuery = @"CREATE TABLE IF NOT EXISTS [Productos] (
                                                [Codigo] TEXT NOT NULL PRIMARY KEY,
                                                [Nombre] TEXT NOT NULL,
                                                [Precio] REAL NOT NULL,
                                                [Costo] REAL DEFAULT 0,
                                                [Stock] INTEGER NOT NULL,
                                                [StockMinimo] INTEGER DEFAULT 5,
                                                [CategoriaId] INTEGER,
                                                FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createProductosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Alter Productos table to add new columns if they don't exist (for existing databases)
                    try
                    {
                        string alterProductosCosto = "ALTER TABLE Productos ADD COLUMN Costo REAL DEFAULT 0";
                        using (var cmd = new SQLiteCommand(alterProductosCosto, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterProductosStockMinimo = "ALTER TABLE Productos ADD COLUMN StockMinimo INTEGER DEFAULT 5";
                        using (var cmd = new SQLiteCommand(alterProductosStockMinimo, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterProductosCategoria = "ALTER TABLE Productos ADD COLUMN CategoriaId INTEGER";
                        using (var cmd = new SQLiteCommand(alterProductosCategoria, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

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
                                                [TotalEfectivo] REAL DEFAULT 0,
                                                [TotalTarjeta] REAL DEFAULT 0,
                                                [TotalTransferencia] REAL DEFAULT 0,
                                                [Notas] TEXT,
                                                [Estado] TEXT NOT NULL,
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createTurnosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Alter Turnos table to add new columns if they don't exist
                    try
                    {
                        string alterTurnosEfectivo = "ALTER TABLE Turnos ADD COLUMN TotalEfectivo REAL DEFAULT 0";
                        using (var cmd = new SQLiteCommand(alterTurnosEfectivo, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterTurnosTarjeta = "ALTER TABLE Turnos ADD COLUMN TotalTarjeta REAL DEFAULT 0";
                        using (var cmd = new SQLiteCommand(alterTurnosTarjeta, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterTurnosTransferencia = "ALTER TABLE Turnos ADD COLUMN TotalTransferencia REAL DEFAULT 0";
                        using (var cmd = new SQLiteCommand(alterTurnosTransferencia, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterTurnosNotas = "ALTER TABLE Turnos ADD COLUMN Notas TEXT";
                        using (var cmd = new SQLiteCommand(alterTurnosNotas, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

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
                                                [MetodoPago] TEXT DEFAULT 'Efectivo',
                                                FOREIGN KEY ([TurnoId]) REFERENCES [Turnos]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createVentasQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Alter Ventas table to add MetodoPago column if it doesn't exist
                    try
                    {
                        string alterVentasMetodoPago = "ALTER TABLE Ventas ADD COLUMN MetodoPago TEXT DEFAULT 'Efectivo'";
                        using (var cmd = new SQLiteCommand(alterVentasMetodoPago, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

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

                    // Create MovimientosCaja table (for expenses, withdrawals, deposits)
                    string createMovimientosQuery = @"CREATE TABLE IF NOT EXISTS [MovimientosCaja] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [TurnoId] INTEGER NOT NULL,
                                                [Tipo] TEXT NOT NULL,
                                                [Monto] REAL NOT NULL,
                                                [Concepto] TEXT NOT NULL,
                                                [Fecha] TEXT NOT NULL,
                                                [UsuarioId] INTEGER NOT NULL,
                                                [UsuarioNombre] TEXT NOT NULL,
                                                FOREIGN KEY ([TurnoId]) REFERENCES [Turnos]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createMovimientosQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Configuracion table (store settings)
                    string createConfiguracionQuery = @"CREATE TABLE IF NOT EXISTS [Configuracion] (
                                                [Id] INTEGER PRIMARY KEY CHECK ([Id] = 1),
                                                [NombreTienda] TEXT NOT NULL DEFAULT 'TuTiendita',
                                                [RUC] TEXT,
                                                [Direccion] TEXT,
                                                [Telefono] TEXT,
                                                [Email] TEXT,
                                                [Logo] BLOB,
                                                [IVA] REAL NOT NULL DEFAULT 0.0,
                                                [MensajePiePagina] TEXT DEFAULT 'Gracias por su compra',
                                                [MonedaSimbolo] TEXT DEFAULT '$',
                                                [BackupAutomatico] INTEGER DEFAULT 1,
                                                [IntervalolBackupHoras] INTEGER DEFAULT 24
                                                )";
                    using (var cmd = new SQLiteCommand(createConfiguracionQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create AuditLog table (audit trail)
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
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createAuditLogQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Promociones table (discounts and promotions)
                    string createPromocionesQuery = @"CREATE TABLE IF NOT EXISTS [Promociones] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Nombre] TEXT NOT NULL,
                                                [Descripcion] TEXT,
                                                [TipoDescuento] TEXT NOT NULL,
                                                [ValorDescuento] REAL NOT NULL,
                                                [ProductoId] INTEGER,
                                                [CodigoCupon] TEXT UNIQUE,
                                                [FechaInicio] TEXT NOT NULL,
                                                [FechaFin] TEXT NOT NULL,
                                                [Activo] INTEGER DEFAULT 1,
                                                [MontoMinimo] REAL DEFAULT 0,
                                                FOREIGN KEY ([ProductoId]) REFERENCES [Productos]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createPromocionesQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Clientes table (customer CRM)
                    string createClientesQuery = @"CREATE TABLE IF NOT EXISTS [Clientes] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Nombre] TEXT NOT NULL,
                                                [Apellido] TEXT,
                                                [Documento] TEXT UNIQUE,
                                                [Telefono] TEXT,
                                                [Email] TEXT,
                                                [Direccion] TEXT,
                                                [FechaNacimiento] TEXT,
                                                [LimiteCredito] REAL DEFAULT 0,
                                                [DescuentoEspecial] REAL DEFAULT 0,
                                                [FechaRegistro] TEXT NOT NULL,
                                                [Activo] INTEGER DEFAULT 1,
                                                [Notas] TEXT
                                                )";
                    using (var cmd = new SQLiteCommand(createClientesQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create CreditoClientes table (customer credit/debts)
                    string createCreditoQuery = @"CREATE TABLE IF NOT EXISTS [CreditoClientes] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [ClienteId] INTEGER NOT NULL,
                                                [VentaId] INTEGER NOT NULL,
                                                [MontoTotal] REAL NOT NULL,
                                                [MontoAbonado] REAL DEFAULT 0,
                                                [MontoPendiente] REAL NOT NULL,
                                                [FechaVenta] TEXT NOT NULL,
                                                [FechaVencimiento] TEXT,
                                                [Estado] TEXT DEFAULT 'Pendiente',
                                                FOREIGN KEY ([ClienteId]) REFERENCES [Clientes]([Id]),
                                                FOREIGN KEY ([VentaId]) REFERENCES [Ventas]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createCreditoQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create PagosCredito table (credit payments)
                    string createPagosCreditoQuery = @"CREATE TABLE IF NOT EXISTS [PagosCredito] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [CreditoId] INTEGER NOT NULL,
                                                [Monto] REAL NOT NULL,
                                                [FechaPago] TEXT NOT NULL,
                                                [MetodoPago] TEXT NOT NULL,
                                                [UsuarioId] INTEGER,
                                                [Notas] TEXT,
                                                FOREIGN KEY ([CreditoId]) REFERENCES [CreditoClientes]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createPagosCreditoQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create Proveedores table (suppliers)
                    string createProveedoresQuery = @"CREATE TABLE IF NOT EXISTS [Proveedores] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [Nombre] TEXT NOT NULL,
                                                [Contacto] TEXT,
                                                [Telefono] TEXT,
                                                [Email] TEXT,
                                                [Direccion] TEXT,
                                                [RUC] TEXT UNIQUE,
                                                [Activo] INTEGER DEFAULT 1,
                                                [Notas] TEXT
                                                )";
                    using (var cmd = new SQLiteCommand(createProveedoresQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create OrdenesCompra table (purchase orders)
                    string createOrdenesCompraQuery = @"CREATE TABLE IF NOT EXISTS [OrdenesCompra] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [ProveedorId] INTEGER NOT NULL,
                                                [FechaOrden] TEXT NOT NULL,
                                                [FechaEntrega] TEXT,
                                                [Total] REAL NOT NULL,
                                                [Estado] TEXT DEFAULT 'Pendiente',
                                                [UsuarioId] INTEGER,
                                                [Notas] TEXT,
                                                FOREIGN KEY ([ProveedorId]) REFERENCES [Proveedores]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createOrdenesCompraQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create DetalleOrdenCompra table
                    string createDetalleOrdenQuery = @"CREATE TABLE IF NOT EXISTS [DetalleOrdenCompra] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [OrdenCompraId] INTEGER NOT NULL,
                                                [ProductoId] INTEGER NOT NULL,
                                                [Cantidad] INTEGER NOT NULL,
                                                [PrecioUnitario] REAL NOT NULL,
                                                [Subtotal] REAL NOT NULL,
                                                FOREIGN KEY ([OrdenCompraId]) REFERENCES [OrdenesCompra]([Id]),
                                                FOREIGN KEY ([ProductoId]) REFERENCES [Productos]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createDetalleOrdenQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                // Create default data if database is new
                if (isNewDatabase)
                {
                    // Create default admin user
                    string insertAdminQuery = @"INSERT INTO [Usuarios] ([Nombre], [Contrasena], [NivelAcceso])
                                                VALUES (@nombre, @contrasena, @nivel)";
                    using (var cmd = new SQLiteCommand(insertAdminQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@nombre", "admin");
                        cmd.Parameters.AddWithValue("@contrasena", "admin123");
                        cmd.Parameters.AddWithValue("@nivel", "Gerente");
                        cmd.ExecuteNonQuery();
                    }

                    // Create default categories
                    string[] categorias = { "Bebidas", "Abarrotes", "Lácteos", "Panadería", "Limpieza", "Snacks", "General" };
                    foreach (var cat in categorias)
                    {
                        string insertCatQuery = "INSERT INTO [Categorias] ([Nombre]) VALUES (@nombre)";
                        using (var cmd = new SQLiteCommand(insertCatQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@nombre", cat);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Create default configuration
                    string insertConfigQuery = @"INSERT INTO [Configuracion]
                                                ([Id], [NombreTienda], [IVA], [MensajePiePagina], [MonedaSimbolo])
                                                VALUES (1, 'TuTiendita', 0.0, 'Gracias por su compra', '$')";
                    using (var cmd = new SQLiteCommand(insertConfigQuery, connection))
                    {
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