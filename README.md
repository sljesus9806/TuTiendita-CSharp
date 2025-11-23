# TuTiendita - POS System for Windows

A simple yet powerful Point of Sale (POS) system built with C# and WPF for Windows 10+. This application provides a complete solution for managing sales, inventory, shifts, users, and generating reports.

## Features

### Core Functionality
- ✅ **User Management** - Create and manage users with role-based access (Gerente/Manager and Cajero/Cashier)
- ✅ **Inventory Management** - Complete CRUD operations for products with real-time stock tracking
- ✅ **Point of Sale (POS)** - Fast and intuitive sales interface with product search and cart management
- ✅ **Shift Management** - Open/close cash register shifts with automatic sales tracking
- ✅ **Sales Reports** - Comprehensive daily, weekly, and monthly sales reports
- ✅ **Inventory Reports** - Stock levels, low stock alerts, and inventory valuation
- ✅ **Transaction History** - Complete record of all sales with detailed line items

### Technical Features
- 🗄️ **SQLite Database** - Portable, zero-configuration database (no SQL Server installation needed)
- 🖥️ **WPF UI** - Modern, elegant Windows interface
- 🔐 **Role-Based Access** - Different permission levels for managers and cashiers
- 💾 **Automatic Data Persistence** - All transactions saved automatically
- 📊 **Real-time Analytics** - Live sales statistics and inventory tracking

## System Requirements

- **Operating System**: Windows 10 or later
- **.NET Runtime**: .NET 8.0 or later
- **RAM**: 2GB minimum (4GB recommended)
- **Disk Space**: 50MB for application + space for database growth
- **Display**: 1024x768 minimum resolution (1920x1080 recommended)

## Installation

### Prerequisites
1. Download and install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) if not already installed

### Building from Source
1. Clone or download this repository
2. Open `TuTiendita.sln` in Visual Studio 2022 or later
3. Restore NuGet packages (Visual Studio does this automatically)
4. Build the solution (F6 or Build → Build Solution)
5. Run the application (F5 or Debug → Start Debugging)

### First Run
On the first run, the application will:
- Create a SQLite database file (`productos.db`) in the application directory
- Create all necessary database tables automatically
- Create a default admin user:
  - **Username**: `admin`
  - **Password**: `admin123`
  - **Role**: Gerente (Manager)

⚠️ **Important**: Change the default admin password after first login!

## Database Schema

The system uses SQLite with the following tables:

### Productos (Products)
- `Codigo` (TEXT, PRIMARY KEY) - Product code
- `Nombre` (TEXT) - Product name
- `Precio` (REAL) - Unit price
- `Stock` (INTEGER) - Available quantity

### Usuarios (Users)
- `Id` (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- `Nombre` (TEXT) - Username
- `Contrasena` (TEXT) - Password
- `NivelAcceso` (TEXT) - Access level (Gerente/Cajero)

### Turnos (Shifts)
- `Id` (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- `UsuarioId` (INTEGER, FOREIGN KEY) - User who opened the shift
- `UsuarioNombre` (TEXT) - Username
- `FechaApertura` (TEXT) - Opening date/time
- `FechaCierre` (TEXT) - Closing date/time
- `MontoInicial` (REAL) - Initial cash amount
- `MontoFinal` (REAL) - Final cash amount
- `TotalVentas` (REAL) - Total sales during shift
- `Estado` (TEXT) - Status (Abierto/Cerrado)

### Ventas (Sales)
- `Id` (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- `TurnoId` (INTEGER, FOREIGN KEY) - Associated shift
- `UsuarioId` (INTEGER, FOREIGN KEY) - User who made the sale
- `UsuarioNombre` (TEXT) - Username
- `Fecha` (TEXT) - Sale date/time
- `Total` (REAL) - Total amount
- `MontoPagado` (REAL) - Amount paid by customer
- `Cambio` (REAL) - Change given

### DetalleVentas (Sale Details)
- `Id` (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- `VentaId` (INTEGER, FOREIGN KEY) - Associated sale
- `ProductoCodigo` (TEXT, FOREIGN KEY) - Product code
- `ProductoNombre` (TEXT) - Product name
- `Cantidad` (INTEGER) - Quantity sold
- `PrecioUnitario` (REAL) - Unit price at time of sale
- `Subtotal` (REAL) - Line total

## User Guide

### Login
1. Launch the application
2. Enter username and password
3. Click "Iniciar Sesión" or press Enter

### Making a Sale
1. Click **Ventas** in the sidebar
2. Search for products using the search box
3. Double-click products to add them to the cart
4. Adjust quantities in the cart as needed
5. Click **Vender** to complete the sale
6. Enter the amount paid by the customer
7. The system will calculate change and save the transaction

### Managing Shifts
1. Click **Cerrar Caja** in the sidebar
2. To open a shift:
   - Enter the initial cash amount
   - Click **Abrir Turno**
3. To close a shift:
   - Count the cash in the register
   - Enter the final amount
   - Click **Cerrar Turno**
   - Review the summary (shows any discrepancies)

### Managing Products
1. Click **Productos** in the sidebar
2. View all products in the data grid
3. Use the search box to filter products
4. Click **Agregar** to add new products
5. Double-click a product or select and click **Editar** to modify
6. Select a product and click **Eliminar** to delete

### Managing Users (Gerente only)
1. Click **Usuarios** in the sidebar
2. View all users in the data grid
3. Click **Agregar** to create new users
4. Select a user and click **Editar** to modify
5. Select a user and click **Eliminar** to delete

### Viewing Reports
1. Click **Reportes** in the sidebar
2. **Reporte de Ventas** tab:
   - Select date range (or use quick filters: Hoy, Esta Semana, Este Mes)
   - Click **Buscar** to load sales data
   - View summary cards (Total, # Transactions, Average, Products Sold)
   - Click on any sale to view detailed line items
3. **Detalle de Venta** tab:
   - Shows detailed breakdown of selected sale
   - Lists all products sold in the transaction
4. **Reporte de Inventario** tab:
   - View all products with current stock
   - See total inventory value
   - Identify low stock items (< 10 units)

## User Roles and Permissions

### Gerente (Manager)
- Full access to all features
- Can create/edit/delete users
- Can create other managers
- Can manage shifts
- Can view all reports
- Can manage inventory
- Can process sales

### Cajero (Cashier)
- Can process sales
- Can manage shifts
- Can view inventory (read-only in reports)
- Cannot access user management
- Cannot create manager accounts

## Project Structure

```
TuTiendita-CSharp/
├── Database.cs                          # Database initialization
├── README.md                            # This file
├── TuTiendita.sln                      # Visual Studio solution
└── TuTiendita/
    ├── App.xaml                         # Application configuration
    ├── App.xaml.cs                      # Application startup
    ├── MainWindow.xaml                  # Login window UI
    ├── MainWindow.xaml.cs               # Login logic
    ├── VentanaPrincipal.xaml           # Main window UI
    ├── VentanaPrincipal.xaml.cs        # Main navigation
    ├── VentasUserControl.xaml          # POS interface UI
    ├── VentasUserControl.xaml.cs       # POS logic
    ├── CerrarCajaUserControl.xaml      # Shift management UI
    ├── CerrarCajaUserControl.xaml.cs   # Shift logic
    ├── ProductosUserControl.xaml       # Inventory management UI
    ├── ProductosUserControl.xaml.cs    # Inventory logic
    ├── UsuariosUserControl.xaml        # User management UI
    ├── UsuariosUserControl.xaml.cs     # User logic
    ├── ReportesUserControl.xaml        # Reports UI
    ├── ReportesUserControl.xaml.cs     # Reports logic
    ├── AgregarProductoWindow.xaml      # Add product dialog
    ├── EditarProductoWindow.xaml       # Edit product dialog
    ├── DialogoEntrada.xaml             # Input dialog
    ├── Usuarios.cs                      # User data model
    └── TuTiendita.csproj               # Project configuration
```

## Dependencies

- **.NET 8.0** - Application framework
- **System.Data.SQLite** v1.0.118 - SQLite database engine
- **WPF** - Windows Presentation Foundation (included in .NET)

## Development

### Technology Stack
- **Language**: C# 12
- **Framework**: .NET 8.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: SQLite with ADO.NET
- **Architecture**: MVVM-Lite pattern with code-behind

### Building
```bash
# Using .NET CLI
dotnet restore
dotnet build
dotnet run --project TuTiendita/TuTiendita.csproj

# Using Visual Studio
# Open TuTiendita.sln and press F5
```

### Database Location
The SQLite database file `productos.db` is created in the application's working directory. For development, this is typically the `bin/Debug/net8.0-windows` folder.

## Future Enhancements

Potential features for future versions:
- 📄 PDF receipt generation
- 🔒 Password hashing (currently stores plaintext)
- 👥 Customer management (CRM)
- 📦 Purchase order management
- 📧 Email reports
- 🌐 Multi-language support
- 📱 Barcode scanning support
- 💳 Credit card payment integration
- ☁️ Cloud backup
- 📊 Advanced analytics and charts

## Troubleshooting

### Database Issues
**Problem**: Database file is locked
**Solution**: Close all instances of the application and try again

**Problem**: Tables not created
**Solution**: Delete `productos.db` and restart the application to recreate

### Login Issues
**Problem**: Forgot admin password
**Solution**: Delete `productos.db` to reset (⚠️ this will delete all data)

### Build Issues
**Problem**: NuGet package restore failed
**Solution**: Right-click solution → Restore NuGet Packages

## License

This project is provided as-is for educational and commercial use.

## Support

For issues, questions, or contributions, please contact the development team.

---

**Built with ❤️ using C# and WPF**