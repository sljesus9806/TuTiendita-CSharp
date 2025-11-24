using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TuTiendita
{
    public partial class CerrarCajaUserControl : UserControl
    {
        private Usuario usuarioActual;
        private Turno? turnoActual;

        public CerrarCajaUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            CargarEstadoTurno();
            CargarHistorialTurnos();
            ConfigurarPermisos(); // Configurar permisos según el rol del usuario
        }

        private void ConfigurarPermisos()
        {
            // Si el usuario es Cajero, deshabilitar ciertas funciones
            if (usuarioActual != null && usuarioActual.NivelAcceso == "Cajero")
            {
                // Los cajeros NO pueden registrar movimientos de caja (gastos, retiros, depósitos)
                // Esto previene fraude y manipulación del efectivo
                if (btnRegistrarMovimiento != null)
                {
                    btnRegistrarMovimiento.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CargarEstadoTurno()
        {
            turnoActual = ObtenerTurnoAbierto();

            if (turnoActual != null)
            {
                // Hay un turno abierto
                txtEstadoTurno.Text = $"Turno #{turnoActual.Id} abierto - Usuario: {turnoActual.UsuarioNombre} - Desde: {turnoActual.FechaApertura}";
                pnlAbrirTurno.Visibility = Visibility.Collapsed;
                pnlCerrarTurno.Visibility = Visibility.Visible;
                pnlInfoTurno.Visibility = Visibility.Visible;

                // Mostrar información del turno
                txtMontoInicialActual.Text = turnoActual.MontoInicial.ToString("C");
                txtTotalVentasActual.Text = $"{turnoActual.TotalVentas:C}\n(E:{turnoActual.TotalEfectivo:C} T:{turnoActual.TotalTarjeta:C} Tr:{turnoActual.TotalTransferencia:C})";
                txtUsuarioTurno.Text = turnoActual.UsuarioNombre;
                txtFechaApertura.Text = turnoActual.FechaApertura;
                txtNumeroVentas.Text = ObtenerNumeroVentasTurno(turnoActual.Id).ToString();

                // Cargar movimientos de caja
                CargarMovimientosCaja();
            }
            else
            {
                // No hay turno abierto
                txtEstadoTurno.Text = "No hay turno abierto - Puede abrir un nuevo turno";
                pnlAbrirTurno.Visibility = Visibility.Visible;
                pnlCerrarTurno.Visibility = Visibility.Collapsed;
                pnlInfoTurno.Visibility = Visibility.Collapsed;
            }
        }

        private void CargarMovimientosCaja()
        {
            if (turnoActual == null) return;

            try
            {
                var movimientos = new List<MovimientoCaja>();
                decimal totalMovimientos = 0;

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM MovimientosCaja WHERE TurnoId = @turnoId ORDER BY Fecha DESC";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@turnoId", turnoActual.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var movimiento = new MovimientoCaja
                                {
                                    Id = reader.GetInt32(0),
                                    TurnoId = reader.GetInt32(1),
                                    Tipo = reader.GetString(2),
                                    Monto = reader.GetDecimal(3),
                                    Concepto = reader.GetString(4),
                                    Fecha = reader.GetString(5),
                                    UsuarioId = reader.GetInt32(6),
                                    UsuarioNombre = reader.GetString(7)
                                };
                                movimientos.Add(movimiento);

                                // Calcular total (negativos para gastos y retiros, positivos para depósitos)
                                if (movimiento.Tipo == "Gasto" || movimiento.Tipo == "Retiro")
                                    totalMovimientos -= movimiento.Monto;
                                else
                                    totalMovimientos += movimiento.Monto;
                            }
                        }
                    }
                }

                dgMovimientos.ItemsSource = movimientos;
                txtTotalMovimientos.Text = totalMovimientos.ToString("C");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar movimientos: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRegistrarMovimiento_Click(object sender, RoutedEventArgs e)
        {
            if (turnoActual == null)
            {
                MessageBox.Show("No hay turno abierto.", "Advertencia",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialogo = new DialogoMovimientoCaja();
            if (dialogo.ShowDialog() == true)
            {
                try
                {
                    using (var connection = Database.GetConnection())
                    {
                        connection.Open();
                        string query = @"INSERT INTO MovimientosCaja (TurnoId, Tipo, Monto, Concepto, Fecha, UsuarioId, UsuarioNombre)
                                       VALUES (@turnoId, @tipo, @monto, @concepto, @fecha, @usuarioId, @usuarioNombre)";

                        using (var cmd = new SQLiteCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@turnoId", turnoActual.Id);
                            cmd.Parameters.AddWithValue("@tipo", dialogo.TipoMovimiento);
                            cmd.Parameters.AddWithValue("@monto", dialogo.Monto);
                            cmd.Parameters.AddWithValue("@concepto", dialogo.Concepto);
                            cmd.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@usuarioId", usuarioActual.IdUsuario);
                            cmd.Parameters.AddWithValue("@usuarioNombre", usuarioActual.Nombre);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"Movimiento registrado: {dialogo.TipoMovimiento} de {dialogo.Monto:C}",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarMovimientosCaja();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al registrar movimiento: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Turno ObtenerTurnoAbierto()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Turnos WHERE Estado = 'Abierto' ORDER BY FechaApertura DESC LIMIT 1";
                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Turno
                            {
                                Id = reader.GetInt32(0),
                                UsuarioId = reader.GetInt32(1),
                                UsuarioNombre = reader.GetString(2),
                                FechaApertura = reader.GetString(3),
                                FechaCierre = reader.IsDBNull(4) ? null : reader.GetString(4),
                                MontoInicial = reader.GetDecimal(5),
                                MontoFinal = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                TotalVentas = reader.GetDecimal(7),
                                TotalEfectivo = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetDecimal(8) : 0,
                                TotalTarjeta = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetDecimal(9) : 0,
                                TotalTransferencia = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetDecimal(10) : 0,
                                Notas = reader.FieldCount > 11 && !reader.IsDBNull(11) ? reader.GetString(11) : "",
                                Estado = reader.FieldCount > 12 && !reader.IsDBNull(12) ? reader.GetString(12) : "Abierto"
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener turno abierto: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        private int ObtenerNumeroVentasTurno(int turnoId)
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Ventas WHERE TurnoId = @turnoId";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@turnoId", turnoId);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private decimal CalcularTotalMovimientos()
        {
            if (turnoActual == null) return 0;

            try
            {
                decimal total = 0;
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Tipo, Monto FROM MovimientosCaja WHERE TurnoId = @turnoId";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@turnoId", turnoActual.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string tipo = reader.GetString(0);
                                decimal monto = reader.GetDecimal(1);

                                // Gastos y retiros restan, depósitos suman
                                if (tipo == "Gasto" || tipo == "Retiro")
                                    total -= monto;
                                else
                                    total += monto;
                            }
                        }
                    }
                }
                return total;
            }
            catch
            {
                return 0;
            }
        }

        private List<MovimientoCaja> ObtenerMovimientosTurno(int turnoId)
        {
            var movimientos = new List<MovimientoCaja>();
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM MovimientosCaja WHERE TurnoId = @turnoId ORDER BY Fecha";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@turnoId", turnoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                movimientos.Add(new MovimientoCaja
                                {
                                    Id = reader.GetInt32(0),
                                    TurnoId = reader.GetInt32(1),
                                    Tipo = reader.GetString(2),
                                    Monto = reader.GetDecimal(3),
                                    Concepto = reader.GetString(4),
                                    Fecha = reader.GetString(5),
                                    UsuarioId = reader.GetInt32(6),
                                    UsuarioNombre = reader.GetString(7)
                                });
                            }
                        }
                    }
                }
            }
            catch { }
            return movimientos;
        }

        private void BtnAbrirTurno_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMontoInicial.Text))
            {
                MessageBox.Show("Ingrese el monto inicial en caja.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtMontoInicial.Text, out decimal montoInicial) || montoInicial < 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string insertQuery = @"INSERT INTO Turnos (UsuarioId, UsuarioNombre, FechaApertura, MontoInicial, Estado, TotalVentas)
                                          VALUES (@usuarioId, @usuarioNombre, @fechaApertura, @montoInicial, @estado, 0)";

                    using (var cmd = new SQLiteCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@usuarioId", usuarioActual.IdUsuario);
                        cmd.Parameters.AddWithValue("@usuarioNombre", usuarioActual.Nombre);
                        cmd.Parameters.AddWithValue("@fechaApertura", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@montoInicial", montoInicial);
                        cmd.Parameters.AddWithValue("@estado", "Abierto");
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show($"Turno abierto exitosamente con ${montoInicial:F2}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                txtMontoInicial.Clear();
                CargarEstadoTurno();
                CargarHistorialTurnos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir turno: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrarTurno_Click(object sender, RoutedEventArgs e)
        {
            if (turnoActual == null)
            {
                MessageBox.Show("No hay turno abierto para cerrar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Abrir diálogo de contador de denominaciones
            var dialogoCierre = new DialogoCierreCaja();
            if (dialogoCierre.ShowDialog() != true)
            {
                return; // Usuario canceló
            }

            decimal montoFinal = dialogoCierre.MontoFinalContado;
            string notas = dialogoCierre.NotasCierre;

            // Calcular total de movimientos de caja
            decimal totalMovimientos = CalcularTotalMovimientos();

            // Calcular montos esperados (incluyendo movimientos)
            decimal montoEsperadoEfectivo = turnoActual.MontoInicial + turnoActual.TotalEfectivo + totalMovimientos;
            decimal diferencia = montoFinal - montoEsperadoEfectivo;

            // Mostrar resumen con desglose
            string mensaje = $"═══════════════════════════════════════\n" +
                           $"  RESUMEN DE CIERRE - TURNO #{turnoActual.Id}\n" +
                           $"═══════════════════════════════════════\n\n" +
                           $"APERTURA:\n" +
                           $"  Monto Inicial: {turnoActual.MontoInicial:C}\n\n" +
                           $"VENTAS POR MÉTODO DE PAGO:\n" +
                           $"  • Efectivo:       {turnoActual.TotalEfectivo:C}\n" +
                           $"  • Tarjeta:        {turnoActual.TotalTarjeta:C}\n" +
                           $"  • Transferencia:  {turnoActual.TotalTransferencia:C}\n" +
                           $"  ─────────────────────────────────\n" +
                           $"  Total Ventas:     {turnoActual.TotalVentas:C}\n\n" +
                           $"MOVIMIENTOS DE CAJA:\n" +
                           $"  Total Movimientos: {totalMovimientos:C}\n\n" +
                           $"CIERRE:\n" +
                           $"  Efectivo Esperado: {montoEsperadoEfectivo:C}\n" +
                           $"  Efectivo Contado:  {montoFinal:C}\n" +
                           $"  Diferencia:        {diferencia:C}\n\n";

            if (diferencia > 0)
                mensaje += "  ⚠️  HAY UN SOBRANTE EN CAJA\n";
            else if (diferencia < 0)
                mensaje += "  ⚠️  HAY UN FALTANTE EN CAJA\n";
            else
                mensaje += "  ✓  CAJA CUADRADA\n";

            if (!string.IsNullOrWhiteSpace(notas))
                mensaje += $"\nNotas: {notas}\n";

            mensaje += "\n¿Desea cerrar el turno?";

            var result = MessageBox.Show(mensaje, "Confirmar Cierre de Turno", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var connection = Database.GetConnection())
                    {
                        connection.Open();
                        string updateQuery = @"UPDATE Turnos
                                             SET FechaCierre = @fechaCierre,
                                                 MontoFinal = @montoFinal,
                                                 Notas = @notas,
                                                 Estado = @estado
                                             WHERE Id = @id";

                        using (var cmd = new SQLiteCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@fechaCierre", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@montoFinal", montoFinal);
                            cmd.Parameters.AddWithValue("@notas", notas ?? "");
                            cmd.Parameters.AddWithValue("@estado", "Cerrado");
                            cmd.Parameters.AddWithValue("@id", turnoActual.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Generar reporte de cierre
                    GenerarReporteCierre(turnoActual.Id, montoFinal, diferencia, notas, totalMovimientos);

                    MessageBox.Show("Turno cerrado exitosamente.\nSe ha generado el reporte de cierre.",
                                  "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtMontoFinal.Clear();
                    CargarEstadoTurno();
                    CargarHistorialTurnos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cerrar turno: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerarReporteCierre(int turnoId, decimal montoFinal, decimal diferencia, string notas, decimal totalMovimientos)
        {
            try
            {
                // Crear directorio de reportes si no existe
                string reportesFolder = "Reportes";
                if (!System.IO.Directory.Exists(reportesFolder))
                {
                    System.IO.Directory.CreateDirectory(reportesFolder);
                }

                // Generar nombre de archivo PDF
                string fileName = $"Reportes/CierreCaja_Turno{turnoId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                // Usar el generador de PDF profesional
                bool exito = Helpers.ReportePdfGenerator.GenerarReporteCierreCaja(turnoId, fileName);

                if (exito)
                {
                    // Registrar en auditoría
                    Helpers.AuditLogger.RegistrarCierreTurno(usuarioActual, turnoId, montoFinal, diferencia);

                    // Preguntar si desea abrir el reporte
                    var resultado = MessageBox.Show(
                        "✓ Reporte de cierre generado exitosamente\n\n¿Desea abrir el reporte PDF?",
                        "Reporte de Cierre",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        Helpers.TicketPdfGenerator.AbrirPdf(fileName);
                    }
                }
                else
                {
                    MessageBox.Show("Error al generar el reporte PDF", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar reporte: {ex.Message}", "Advertencia",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CargarHistorialTurnos()
        {
            try
            {
                var turnos = new List<Turno>();

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Turnos ORDER BY FechaApertura DESC LIMIT 50";
                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            turnos.Add(new Turno
                            {
                                Id = reader.GetInt32(0),
                                UsuarioId = reader.GetInt32(1),
                                UsuarioNombre = reader.GetString(2),
                                FechaApertura = reader.GetString(3),
                                FechaCierre = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                                MontoInicial = reader.GetDecimal(5),
                                MontoFinal = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                TotalVentas = reader.GetDecimal(7),
                                TotalEfectivo = reader.FieldCount > 8 && !reader.IsDBNull(8) ? reader.GetDecimal(8) : 0,
                                TotalTarjeta = reader.FieldCount > 9 && !reader.IsDBNull(9) ? reader.GetDecimal(9) : 0,
                                TotalTransferencia = reader.FieldCount > 10 && !reader.IsDBNull(10) ? reader.GetDecimal(10) : 0,
                                Notas = reader.FieldCount > 11 && !reader.IsDBNull(11) ? reader.GetString(11) : "",
                                Estado = reader.FieldCount > 12 && !reader.IsDBNull(12) ? reader.GetString(12) : "Cerrado"
                            });
                        }
                    }
                }

                dgTurnos.ItemsSource = turnos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Turno : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public string FechaApertura { get; set; } = string.Empty;
        public string FechaCierre { get; set; } = string.Empty;
        public decimal MontoInicial { get; set; }
        public decimal MontoFinal { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalTransferencia { get; set; }
        public string Notas { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public string MontoInicialFormateado => MontoInicial.ToString("C");
        public string MontoFinalFormateado => MontoFinal > 0 ? MontoFinal.ToString("C") : "N/A";
        public string TotalVentasFormateado => TotalVentas.ToString("C");

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class MovimientoCaja
    {
        public int Id { get; set; }
        public int TurnoId { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Gasto", "Retiro", "Depósito"
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;

        public string MontoFormateado => Monto.ToString("C");
        public string TipoColor => Tipo == "Gasto" || Tipo == "Retiro" ? "#E74C3C" : "#27AE60";
    }
}
