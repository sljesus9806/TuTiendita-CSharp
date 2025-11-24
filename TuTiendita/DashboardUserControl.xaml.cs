using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace TuTiendita
{
    public partial class DashboardUserControl : UserControl
    {
        public DashboardUserControl()
        {
            InitializeComponent();
            this.Loaded += DashboardUserControl_Loaded;
        }

        private void DashboardUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ActualizarDashboard();
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            ActualizarDashboard();
        }

        private void ActualizarDashboard()
        {
            try
            {
                txtFechaActual.Text = $"Hoy, {DateTime.Now:dddd, dd 'de' MMMM yyyy}";
                CargarKPIs();
                CargarGraficaVentasPorDia();
                CargarGraficaMetodosPago();
                CargarGraficaProductosTop();
                CargarGraficaVentasPorHora();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar dashboard: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarKPIs()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Ventas de hoy
                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string queryHoy = @"SELECT COALESCE(SUM(Total), 0), COUNT(*)
                                       FROM Ventas
                                       WHERE DATE(Fecha) = @Fecha";

                    decimal ventasHoy = 0;
                    int transaccionesHoy = 0;

                    using (var cmd = new SQLiteCommand(queryHoy, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ventasHoy = reader.GetDecimal(0);
                                transaccionesHoy = reader.GetInt32(1);
                            }
                        }
                    }

                    // Ventas de ayer (para comparación)
                    string ayer = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    decimal ventasAyer = 0;

                    using (var cmd = new SQLiteCommand(queryHoy, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", ayer);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ventasAyer = reader.GetDecimal(0);
                            }
                        }
                    }

                    // Ventas del mes
                    string inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
                    string finMes = DateTime.Now.ToString("yyyy-MM-dd");
                    string queryMes = @"SELECT COALESCE(SUM(Total), 0)
                                       FROM Ventas
                                       WHERE DATE(Fecha) BETWEEN @Inicio AND @Fin";

                    decimal ventasMes = 0;

                    using (var cmd = new SQLiteCommand(queryMes, connection))
                    {
                        cmd.Parameters.AddWithValue("@Inicio", inicioMes);
                        cmd.Parameters.AddWithValue("@Fin", finMes);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ventasMes = reader.GetDecimal(0);
                            }
                        }
                    }

                    // Ventas del mes anterior (para comparación)
                    DateTime primerDiaMesAnterior = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                    DateTime ultimoDiaMesAnterior = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
                    decimal ventasMesAnterior = 0;

                    using (var cmd = new SQLiteCommand(queryMes, connection))
                    {
                        cmd.Parameters.AddWithValue("@Inicio", primerDiaMesAnterior.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@Fin", ultimoDiaMesAnterior.ToString("yyyy-MM-dd"));
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ventasMesAnterior = reader.GetDecimal(0);
                            }
                        }
                    }

                    // Productos vendidos hoy
                    string queryProductos = @"SELECT COALESCE(SUM(dv.Cantidad), 0)
                                            FROM DetalleVenta dv
                                            INNER JOIN Ventas v ON dv.VentaId = v.Id
                                            WHERE DATE(v.Fecha) = @Fecha";

                    int productosVendidos = 0;

                    using (var cmd = new SQLiteCommand(queryProductos, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                productosVendidos = reader.GetInt32(0);
                            }
                        }
                    }

                    // Productos con stock bajo (menos de 10)
                    string queryStockBajo = "SELECT COUNT(*) FROM Productos WHERE Stock < 10";
                    int stockBajo = 0;

                    using (var cmd = new SQLiteCommand(queryStockBajo, connection))
                    {
                        stockBajo = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Actualizar UI
                    txtVentasHoy.Text = ventasHoy.ToString("C");
                    txtVentasMes.Text = ventasMes.ToString("C");
                    txtTicketPromedio.Text = transaccionesHoy > 0 ? (ventasHoy / transaccionesHoy).ToString("C") : "$0.00";
                    txtTransaccionesHoy.Text = transaccionesHoy.ToString();
                    txtProductosVendidos.Text = productosVendidos.ToString();
                    txtStockBajo.Text = stockBajo.ToString();

                    // Calcular variaciones
                    if (ventasAyer > 0)
                    {
                        decimal variacionHoy = ((ventasHoy - ventasAyer) / ventasAyer) * 100;
                        txtVariacionHoy.Text = $"{(variacionHoy >= 0 ? "↑" : "↓")} {Math.Abs(variacionHoy):F1}%";
                        txtVariacionHoy.Foreground = variacionHoy >= 0 ?
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)) :
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                    }

                    if (ventasMesAnterior > 0)
                    {
                        decimal variacionMes = ((ventasMes - ventasMesAnterior) / ventasMesAnterior) * 100;
                        txtVariacionMes.Text = $"{(variacionMes >= 0 ? "↑" : "↓")} {Math.Abs(variacionMes):F1}%";
                        txtVariacionMes.Foreground = variacionMes >= 0 ?
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)) :
                            new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar KPIs: {ex.Message}");
            }
        }

        private void CargarGraficaVentasPorDia()
        {
            try
            {
                var ventas = new List<(string fecha, decimal total)>();

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Obtener ventas de los últimos 7 días
                    for (int i = 6; i >= 0; i--)
                    {
                        DateTime fecha = DateTime.Now.AddDays(-i);
                        string fechaStr = fecha.ToString("yyyy-MM-dd");

                        string query = @"SELECT COALESCE(SUM(Total), 0)
                                       FROM Ventas
                                       WHERE DATE(Fecha) = @Fecha";

                        using (var cmd = new SQLiteCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Fecha", fechaStr);
                            decimal total = Convert.ToDecimal(cmd.ExecuteScalar());
                            ventas.Add((fecha.ToString("dd/MM"), total));
                        }
                    }
                }

                // Crear series para el gráfico
                chartVentasPorDia.Series = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = ventas.Select(v => v.total).ToArray(),
                        Name = "Ventas",
                        Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(50)),
                        Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                        GeometrySize = 10,
                        GeometryFill = new SolidColorPaint(SKColors.Blue),
                        GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 3 }
                    }
                };

                chartVentasPorDia.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = ventas.Select(v => v.fecha).ToArray(),
                        LabelsRotation = 0
                    }
                };

                chartVentasPorDia.YAxes = new[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("C0")
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar gráfica de ventas por día: {ex.Message}");
            }
        }

        private void CargarGraficaMetodosPago()
        {
            try
            {
                var metodos = new Dictionary<string, decimal>();

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");

                    // Obtener totales por método de pago desde los turnos de hoy
                    string query = @"SELECT
                                    COALESCE(SUM(TotalEfectivo), 0) as Efectivo,
                                    COALESCE(SUM(TotalTarjeta), 0) as Tarjeta,
                                    COALESCE(SUM(TotalTransferencia), 0) as Transferencia
                                    FROM Turnos
                                    WHERE DATE(FechaApertura) = @Fecha";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                metodos["Efectivo"] = reader.GetDecimal(0);
                                metodos["Tarjeta"] = reader.GetDecimal(1);
                                metodos["Transferencia"] = reader.GetDecimal(2);
                            }
                        }
                    }
                }

                // Filtrar métodos con valor > 0
                var metodosConValor = metodos.Where(m => m.Value > 0).ToList();

                if (metodosConValor.Any())
                {
                    chartMetodosPago.Series = metodosConValor.Select(metodo =>
                        new PieSeries<decimal>
                        {
                            Values = new[] { metodo.Value },
                            Name = $"{metodo.Key} ({metodo.Value:C})"
                        }
                    ).Cast<ISeries>().ToArray();
                }
                else
                {
                    // Datos de ejemplo si no hay ventas
                    chartMetodosPago.Series = new ISeries[]
                    {
                        new PieSeries<decimal> { Values = new[] { 1m }, Name = "Sin datos" }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar gráfica de métodos de pago: {ex.Message}");
            }
        }

        private void CargarGraficaProductosTop()
        {
            try
            {
                var productos = new List<(string nombre, int cantidad)>();

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Top 5 productos más vendidos del mes
                    string inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("yyyy-MM-dd");
                    string query = @"SELECT p.Nombre, SUM(dv.Cantidad) as Total
                                   FROM DetalleVenta dv
                                   INNER JOIN Productos p ON dv.ProductoId = p.Id
                                   INNER JOIN Ventas v ON dv.VentaId = v.Id
                                   WHERE DATE(v.Fecha) >= @Inicio
                                   GROUP BY p.Nombre
                                   ORDER BY Total DESC
                                   LIMIT 5";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Inicio", inicioMes);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                productos.Add((reader.GetString(0), reader.GetInt32(1)));
                            }
                        }
                    }
                }

                if (productos.Any())
                {
                    chartProductosTop.Series = new ISeries[]
                    {
                        new ColumnSeries<int>
                        {
                            Values = productos.Select(p => p.cantidad).ToArray(),
                            Name = "Cantidad Vendida",
                            Fill = new SolidColorPaint(SKColors.MediumSeaGreen)
                        }
                    };

                    chartProductosTop.XAxes = new[]
                    {
                        new Axis
                        {
                            Labels = productos.Select(p => p.nombre.Length > 15 ? p.nombre.Substring(0, 15) + "..." : p.nombre).ToArray(),
                            LabelsRotation = 15
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar gráfica de productos top: {ex.Message}");
            }
        }

        private void CargarGraficaVentasPorHora()
        {
            try
            {
                var ventasPorHora = new Dictionary<int, decimal>();

                // Inicializar todas las horas (0-23) con 0
                for (int i = 0; i < 24; i++)
                {
                    ventasPorHora[i] = 0;
                }

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    string hoy = DateTime.Now.ToString("yyyy-MM-dd");
                    string query = @"SELECT Fecha, Total
                                   FROM Ventas
                                   WHERE DATE(Fecha) = @Fecha";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Fecha", hoy);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (DateTime.TryParse(reader.GetString(0), out DateTime fecha))
                                {
                                    int hora = fecha.Hour;
                                    decimal total = reader.GetDecimal(1);
                                    ventasPorHora[hora] += total;
                                }
                            }
                        }
                    }
                }

                // Crear series
                chartVentasPorHora.Series = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = ventasPorHora.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray(),
                        Name = "Ventas",
                        Fill = new SolidColorPaint(SKColors.DarkOrange)
                    }
                };

                chartVentasPorHora.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = Enumerable.Range(0, 24).Select(h => $"{h}:00").ToArray(),
                        LabelsRotation = 45
                    }
                };

                chartVentasPorHora.YAxes = new[]
                {
                    new Axis
                    {
                        Labeler = value => value.ToString("C0")
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar gráfica de ventas por hora: {ex.Message}");
            }
        }
    }
}
