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

                    // Alter Ventas table to add ClienteId column if it doesn't exist
                    try
                    {
                        string alterVentasClienteId = "ALTER TABLE Ventas ADD COLUMN ClienteId INTEGER REFERENCES Clientes(Id)";
                        using (var cmd = new SQLiteCommand(alterVentasClienteId, connection))
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
                                                [TipoPago] TEXT DEFAULT 'Contado',
                                                [DiasCredito] INTEGER DEFAULT 0,
                                                [FechaVencimiento] TEXT,
                                                FOREIGN KEY ([ProveedorId]) REFERENCES [Proveedores]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createOrdenesCompraQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Alter OrdenesCompra table to add new payment columns if they don't exist
                    try
                    {
                        string alterOrdenTipoPago = "ALTER TABLE OrdenesCompra ADD COLUMN TipoPago TEXT DEFAULT 'Contado'";
                        using (var cmd = new SQLiteCommand(alterOrdenTipoPago, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterOrdenDiasCredito = "ALTER TABLE OrdenesCompra ADD COLUMN DiasCredito INTEGER DEFAULT 0";
                        using (var cmd = new SQLiteCommand(alterOrdenDiasCredito, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterOrdenFechaVencimiento = "ALTER TABLE OrdenesCompra ADD COLUMN FechaVencimiento TEXT";
                        using (var cmd = new SQLiteCommand(alterOrdenFechaVencimiento, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Create DetalleOrdenCompra table
                    string createDetalleOrdenQuery = @"CREATE TABLE IF NOT EXISTS [DetalleOrdenCompra] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [OrdenCompraId] INTEGER NOT NULL,
                                                [ProductoCodigo] TEXT NOT NULL,
                                                [Cantidad] INTEGER NOT NULL,
                                                [PrecioUnitario] REAL NOT NULL,
                                                [Subtotal] REAL NOT NULL,
                                                FOREIGN KEY ([OrdenCompraId]) REFERENCES [OrdenesCompra]([Id]),
                                                FOREIGN KEY ([ProductoCodigo]) REFERENCES [Productos]([Codigo])
                                                )";
                    using (var cmd = new SQLiteCommand(createDetalleOrdenQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // =====================================================
                    // MÓDULO DE FACTURACIÓN ELECTRÓNICA CFDI 4.0 - MÉXICO
                    // =====================================================

                    // Create ConfiguracionFiscal table (tax/fiscal configuration)
                    string createConfiguracionFiscalQuery = @"CREATE TABLE IF NOT EXISTS [ConfiguracionFiscal] (
                                                [Id] INTEGER PRIMARY KEY CHECK ([Id] = 1),
                                                [RFC] TEXT NOT NULL,
                                                [RazonSocial] TEXT NOT NULL,
                                                [RegimenFiscalClave] TEXT NOT NULL,
                                                [CodigoPostal] TEXT NOT NULL,
                                                [Calle] TEXT,
                                                [NumeroExterior] TEXT,
                                                [NumeroInterior] TEXT,
                                                [Colonia] TEXT,
                                                [Municipio] TEXT,
                                                [Estado] TEXT,
                                                [Pais] TEXT DEFAULT 'MEX',
                                                [CertificadoCSD] TEXT,
                                                [LlaveCSD] TEXT,
                                                [ContrasenaLlaveCSD] TEXT,
                                                [PAC] TEXT,
                                                [PACUsuario] TEXT,
                                                [PACContrasena] TEXT,
                                                [PACModoProduccion] INTEGER DEFAULT 0,
                                                [LugarExpedicion] TEXT,
                                                [SerieFactura] TEXT DEFAULT 'A',
                                                [UltimoFolio] INTEGER DEFAULT 0,
                                                [LogoEmpresa] BLOB,
                                                [Activo] INTEGER DEFAULT 1,
                                                [FechaActualizacion] TEXT
                                                )";
                    using (var cmd = new SQLiteCommand(createConfiguracionFiscalQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create CFDI table (electronic invoices)
                    string createCFDIQuery = @"CREATE TABLE IF NOT EXISTS [CFDI] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [UUID] TEXT UNIQUE,
                                                [Serie] TEXT,
                                                [Folio] TEXT NOT NULL,
                                                [Fecha] TEXT NOT NULL,
                                                [FormaPagoClave] TEXT NOT NULL,
                                                [MetodoPagoClave] TEXT NOT NULL,
                                                [TipoComprobante] TEXT NOT NULL DEFAULT 'I',
                                                [Exportacion] TEXT DEFAULT '01',
                                                [Moneda] TEXT DEFAULT 'MXN',
                                                [TipoCambio] REAL DEFAULT 1,
                                                [LugarExpedicion] TEXT NOT NULL,
                                                [Subtotal] REAL NOT NULL,
                                                [Descuento] REAL DEFAULT 0,
                                                [IVATrasladado] REAL DEFAULT 0,
                                                [IVARetenido] REAL DEFAULT 0,
                                                [ISRRetenido] REAL DEFAULT 0,
                                                [Total] REAL NOT NULL,
                                                [EmisorRFC] TEXT NOT NULL,
                                                [EmisorNombre] TEXT NOT NULL,
                                                [EmisorRegimenFiscal] TEXT NOT NULL,
                                                [ReceptorRFC] TEXT NOT NULL,
                                                [ReceptorNombre] TEXT NOT NULL,
                                                [ReceptorRegimenFiscal] TEXT,
                                                [ReceptorDomicilioFiscalCP] TEXT NOT NULL,
                                                [ReceptorUsoCFDI] TEXT NOT NULL,
                                                [VentaId] INTEGER,
                                                [ClienteId] INTEGER,
                                                [CadenaOriginal] TEXT,
                                                [SelloDigitalCFDI] TEXT,
                                                [SelloSAT] TEXT,
                                                [NoCertificadoEmisor] TEXT,
                                                [NoCertificadoSAT] TEXT,
                                                [FechaTimbrado] TEXT,
                                                [XMLOriginal] TEXT,
                                                [XMLTimbrado] TEXT,
                                                [Estado] TEXT DEFAULT 'Pendiente',
                                                [MotivoCancelacion] TEXT,
                                                [FechaCancelacion] TEXT,
                                                [UUIDSustituto] TEXT,
                                                [Notas] TEXT,
                                                [UsuarioId] INTEGER,
                                                [FechaCreacion] TEXT NOT NULL,
                                                FOREIGN KEY ([VentaId]) REFERENCES [Ventas]([Id]),
                                                FOREIGN KEY ([ClienteId]) REFERENCES [Clientes]([Id]),
                                                FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios]([Id])
                                                )";
                    using (var cmd = new SQLiteCommand(createCFDIQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create DetalleCFDI table (invoice line items/concepts)
                    string createDetalleCFDIQuery = @"CREATE TABLE IF NOT EXISTS [DetalleCFDI] (
                                                [Id] INTEGER PRIMARY KEY AUTOINCREMENT,
                                                [CFDIId] INTEGER NOT NULL,
                                                [ClaveProdServ] TEXT NOT NULL,
                                                [NoIdentificacion] TEXT,
                                                [ClaveUnidad] TEXT NOT NULL,
                                                [Unidad] TEXT,
                                                [Descripcion] TEXT NOT NULL,
                                                [Cantidad] REAL NOT NULL,
                                                [ValorUnitario] REAL NOT NULL,
                                                [Importe] REAL NOT NULL,
                                                [Descuento] REAL DEFAULT 0,
                                                [ObjetoImpClave] TEXT DEFAULT '02',
                                                [ImpuestoTrasladado] TEXT,
                                                [TasaOCuota] REAL,
                                                [TipoFactor] TEXT,
                                                [ImporteImpuesto] REAL DEFAULT 0,
                                                FOREIGN KEY ([CFDIId]) REFERENCES [CFDI]([Id]) ON DELETE CASCADE
                                                )";
                    using (var cmd = new SQLiteCommand(createDetalleCFDIQuery, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Alter Clientes table to add fiscal data columns
                    try
                    {
                        string alterClientesRFC = "ALTER TABLE Clientes ADD COLUMN RFC TEXT";
                        using (var cmd = new SQLiteCommand(alterClientesRFC, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterClientesRazonSocial = "ALTER TABLE Clientes ADD COLUMN RazonSocial TEXT";
                        using (var cmd = new SQLiteCommand(alterClientesRazonSocial, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterClientesRegimenFiscal = "ALTER TABLE Clientes ADD COLUMN RegimenFiscalClave TEXT";
                        using (var cmd = new SQLiteCommand(alterClientesRegimenFiscal, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterClientesDomicilioFiscalCP = "ALTER TABLE Clientes ADD COLUMN DomicilioFiscalCP TEXT";
                        using (var cmd = new SQLiteCommand(alterClientesDomicilioFiscalCP, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterClientesUsoCFDI = "ALTER TABLE Clientes ADD COLUMN UsoCFDIDefault TEXT DEFAULT 'G03'";
                        using (var cmd = new SQLiteCommand(alterClientesUsoCFDI, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    // Alter Productos table to add SAT catalog keys
                    try
                    {
                        string alterProductosClaveSAT = "ALTER TABLE Productos ADD COLUMN ClaveProdServSAT TEXT DEFAULT '01010101'";
                        using (var cmd = new SQLiteCommand(alterProductosClaveSAT, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterProductosClaveUnidad = "ALTER TABLE Productos ADD COLUMN ClaveUnidadSAT TEXT DEFAULT 'H87'";
                        using (var cmd = new SQLiteCommand(alterProductosClaveUnidad, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterProductosObjetoImp = "ALTER TABLE Productos ADD COLUMN ObjetoImpClave TEXT DEFAULT '02'";
                        using (var cmd = new SQLiteCommand(alterProductosObjetoImp, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

                    try
                    {
                        string alterProductosTasaIVA = "ALTER TABLE Productos ADD COLUMN TasaIVA REAL DEFAULT 0.16";
                        using (var cmd = new SQLiteCommand(alterProductosTasaIVA, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { /* Column already exists */ }

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

        public static string GetDatabasePath()
        {
            return "productos.db";
        }
    }
}