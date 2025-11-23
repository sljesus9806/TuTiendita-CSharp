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
        private Turno turnoActual;

        public CerrarCajaUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            CargarEstadoTurno();
            CargarHistorialTurnos();
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
                txtTotalVentasActual.Text = turnoActual.TotalVentas.ToString("C");
                txtUsuarioTurno.Text = turnoActual.UsuarioNombre;
                txtFechaApertura.Text = turnoActual.FechaApertura;
                txtNumeroVentas.Text = ObtenerNumeroVentasTurno(turnoActual.Id).ToString();
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
                                Estado = reader.GetString(8)
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

            if (string.IsNullOrWhiteSpace(txtMontoFinal.Text))
            {
                MessageBox.Show("Ingrese el monto final en caja.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtMontoFinal.Text, out decimal montoFinal) || montoFinal < 0)
            {
                MessageBox.Show("Ingrese un monto válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            decimal montoEsperado = turnoActual.MontoInicial + turnoActual.TotalVentas;
            decimal diferencia = montoFinal - montoEsperado;

            string mensaje = $"Resumen del Turno #{turnoActual.Id}\n\n" +
                           $"Monto Inicial: {turnoActual.MontoInicial:C}\n" +
                           $"Total Ventas: {turnoActual.TotalVentas:C}\n" +
                           $"Monto Esperado: {montoEsperado:C}\n" +
                           $"Monto Final: {montoFinal:C}\n" +
                           $"Diferencia: {diferencia:C}\n\n" +
                           (diferencia != 0 ? (diferencia > 0 ? "⚠️ Hay un sobrante en caja" : "⚠️ Hay un faltante en caja") : "✓ Caja cuadrada") +
                           "\n\n¿Desea cerrar el turno?";

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
                                                 Estado = @estado
                                             WHERE Id = @id";

                        using (var cmd = new SQLiteCommand(updateQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@fechaCierre", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            cmd.Parameters.AddWithValue("@montoFinal", montoFinal);
                            cmd.Parameters.AddWithValue("@estado", "Cerrado");
                            cmd.Parameters.AddWithValue("@id", turnoActual.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Turno cerrado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
                                Estado = reader.GetString(8)
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
        public string UsuarioNombre { get; set; }
        public string FechaApertura { get; set; }
        public string FechaCierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal MontoFinal { get; set; }
        public decimal TotalVentas { get; set; }
        public string Estado { get; set; }

        public string MontoInicialFormateado => MontoInicial.ToString("C");
        public string MontoFinalFormateado => MontoFinal > 0 ? MontoFinal.ToString("C") : "N/A";
        public string TotalVentasFormateado => TotalVentas.ToString("C");

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
