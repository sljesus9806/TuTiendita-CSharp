using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class ConfiguracionUserControl : UserControl
    {
        private Usuario usuarioActual;

        public ConfiguracionUserControl(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            this.Loaded += ConfiguracionUserControl_Loaded;
        }

        private void ConfiguracionUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarConfiguracion();
            CargarBackups();
            txtUsuarioActual.Text = usuarioActual.Nombre;
        }

        /// <summary>
        /// Carga la configuración actual desde la base de datos
        /// </summary>
        private void CargarConfiguracion()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"SELECT NombreTienda, RUC, Direccion, Telefono, Email,
                                   IVA, MensajePiePagina, MonedaSimbolo,
                                   BackupAutomatico, IntervaloBackupHoras
                                   FROM Configuracion WHERE Id = 1";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtNombreTienda.Text = reader.GetString(0);
                                txtRUC.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                txtDireccion.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                txtTelefono.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                                txtEmail.Text = reader.IsDBNull(4) ? "" : reader.GetString(4);
                                txtIVA.Text = reader.GetDouble(5).ToString();
                                txtMensajePie.Text = reader.IsDBNull(6) ? "Gracias por su compra" : reader.GetString(6);
                                txtMonedaSimbolo.Text = reader.IsDBNull(7) ? "$" : reader.GetString(7);

                                chkBackupAutomatico.IsChecked = reader.GetInt32(8) == 1;
                                int intervalo = reader.GetInt32(9);
                                cmbIntervaloBackup.SelectedIndex = intervalo switch
                                {
                                    6 => 0,
                                    12 => 1,
                                    24 => 2,
                                    48 => 3,
                                    72 => 4,
                                    _ => 2
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guarda la configuración de la tienda
        /// </summary>
        private void BtnGuardarConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar IVA
                if (!double.TryParse(txtIVA.Text, out double iva) || iva < 0 || iva > 100)
                {
                    MessageBox.Show("El IVA debe ser un número entre 0 y 100", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Obtener valores anteriores para auditoría
                    string queryAnterior = "SELECT NombreTienda, IVA FROM Configuracion WHERE Id = 1";
                    string nombreAnterior = "";
                    double ivaAnterior = 0;

                    using (var cmd = new SQLiteCommand(queryAnterior, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                nombreAnterior = reader.GetString(0);
                                ivaAnterior = reader.GetDouble(1);
                            }
                        }
                    }

                    // Actualizar configuración
                    string query = @"UPDATE Configuracion SET
                                   NombreTienda = @NombreTienda,
                                   RUC = @RUC,
                                   Direccion = @Direccion,
                                   Telefono = @Telefono,
                                   Email = @Email,
                                   IVA = @IVA,
                                   MensajePiePagina = @MensajePie,
                                   MonedaSimbolo = @MonedaSimbolo
                                   WHERE Id = 1";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@NombreTienda", txtNombreTienda.Text);
                        cmd.Parameters.AddWithValue("@RUC", string.IsNullOrWhiteSpace(txtRUC.Text) ? (object)DBNull.Value : txtRUC.Text);
                        cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(txtDireccion.Text) ? (object)DBNull.Value : txtDireccion.Text);
                        cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrWhiteSpace(txtTelefono.Text) ? (object)DBNull.Value : txtTelefono.Text);
                        cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text);
                        cmd.Parameters.AddWithValue("@IVA", iva);
                        cmd.Parameters.AddWithValue("@MensajePie", string.IsNullOrWhiteSpace(txtMensajePie.Text) ? "Gracias por su compra" : txtMensajePie.Text);
                        cmd.Parameters.AddWithValue("@MonedaSimbolo", string.IsNullOrWhiteSpace(txtMonedaSimbolo.Text) ? "$" : txtMonedaSimbolo.Text);

                        cmd.ExecuteNonQuery();
                    }

                    // Registrar cambios en auditoría
                    if (nombreAnterior != txtNombreTienda.Text)
                    {
                        AuditLogger.RegistrarCambioConfiguracion(usuarioActual, "NombreTienda", nombreAnterior, txtNombreTienda.Text);
                    }
                    if (Math.Abs(ivaAnterior - iva) > 0.01)
                    {
                        AuditLogger.RegistrarCambioConfiguracion(usuarioActual, "IVA", ivaAnterior, iva);
                    }
                }

                MessageBox.Show("✓ Configuración guardada correctamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Guarda la configuración de backup automático
        /// </summary>
        private void BtnGuardarConfigBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int intervalo = cmbIntervaloBackup.SelectedIndex switch
                {
                    0 => 6,
                    1 => 12,
                    2 => 24,
                    3 => 48,
                    4 => 72,
                    _ => 24
                };

                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = @"UPDATE Configuracion SET
                                   BackupAutomatico = @BackupAuto,
                                   IntervaloBackupHoras = @Intervalo
                                   WHERE Id = 1";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@BackupAuto", chkBackupAutomatico.IsChecked == true ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Intervalo", intervalo);
                        cmd.ExecuteNonQuery();
                    }
                }

                AuditLogger.RegistrarCambioConfiguracion(usuarioActual, "BackupAutomatico",
                    null, $"{chkBackupAutomatico.IsChecked}, intervalo: {intervalo}h");

                MessageBox.Show("✓ Configuración de backup guardada correctamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración de backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crea un backup manual
        /// </summary>
        private void BtnCrearBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCrearBackup.IsEnabled = false;
                btnCrearBackup.Content = "⏳ Creando backup...";

                string rutaBackup = BackupManager.CrearBackupManual(usuarioActual);

                if (rutaBackup != null)
                {
                    MessageBox.Show($"✓ Backup creado exitosamente:\n{rutaBackup}", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    CargarBackups();
                }
                else
                {
                    MessageBox.Show("Error al crear el backup", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnCrearBackup.IsEnabled = true;
                btnCrearBackup.Content = "📦 Crear Backup Ahora";
            }
        }

        /// <summary>
        /// Abre la carpeta de backups en el explorador
        /// </summary>
        private void BtnAbrirCarpetaBackups_Click(object sender, RoutedEventArgs e)
        {
            BackupManager.AbrirDirectorioBackups();
        }

        /// <summary>
        /// Carga la lista de backups disponibles
        /// </summary>
        private void CargarBackups()
        {
            try
            {
                var backups = BackupManager.ObtenerBackupsDisponibles();
                dgBackups.ItemsSource = backups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar backups: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Actualiza la lista de backups
        /// </summary>
        private void BtnActualizarBackups_Click(object sender, RoutedEventArgs e)
        {
            CargarBackups();
        }

        /// <summary>
        /// Restaura un backup seleccionado
        /// </summary>
        private void BtnRestaurarBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgBackups.SelectedItem == null)
                {
                    MessageBox.Show("Por favor seleccione un backup para restaurar", "Selección requerida",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var backupSeleccionado = (BackupInfo)dgBackups.SelectedItem;

                // Solo administradores pueden restaurar
                if (usuarioActual.NivelAcceso != "Administrador" && usuarioActual.NivelAcceso != "Gerente")
                {
                    MessageBox.Show("Solo los administradores pueden restaurar backups", "Permisos insuficientes",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Restaurar backup");
                    return;
                }

                BackupManager.RestaurarBackup(backupSeleccionado.Ruta, usuarioActual);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al restaurar backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario actual
        /// </summary>
        private void BtnCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(txtPasswordActual.Password))
                {
                    MessageBox.Show("Ingrese su contraseña actual", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPasswordNueva.Password))
                {
                    MessageBox.Show("Ingrese una nueva contraseña", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (txtPasswordNueva.Password != txtPasswordConfirmar.Password)
                {
                    MessageBox.Show("Las contraseñas no coinciden", "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validar complejidad
                var validacion = SecurityHelper.ValidarComplejidadPassword(txtPasswordNueva.Password);
                if (!validacion.esValida)
                {
                    MessageBox.Show(validacion.mensajeError, "Validación",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Verificar contraseña actual
                    string queryPassword = "SELECT Password FROM Usuarios WHERE Id = @Id";
                    string passwordActualDb = "";

                    using (var cmd = new SQLiteCommand(queryPassword, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioActual.Id);
                        passwordActualDb = cmd.ExecuteScalar()?.ToString();
                    }

                    if (string.IsNullOrEmpty(passwordActualDb))
                    {
                        MessageBox.Show("Error al verificar la contraseña actual", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Verificar contraseña actual (compatible con BCrypt y plaintext)
                    bool passwordCorrecta = SecurityHelper.VerifyPasswordCompat(txtPasswordActual.Password, passwordActualDb, out bool esHash);

                    if (!passwordCorrecta)
                    {
                        MessageBox.Show("La contraseña actual es incorrecta", "Error de autenticación",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Actualizar contraseña
                    string passwordHash = SecurityHelper.HashPassword(txtPasswordNueva.Password);
                    string queryUpdate = "UPDATE Usuarios SET Password = @Password WHERE Id = @Id";

                    using (var cmd = new SQLiteCommand(queryUpdate, connection))
                    {
                        cmd.Parameters.AddWithValue("@Password", passwordHash);
                        cmd.Parameters.AddWithValue("@Id", usuarioActual.Id);
                        cmd.ExecuteNonQuery();
                    }

                    // Registrar en auditoría
                    AuditLogger.RegistrarCambioPassword(usuarioActual, usuarioActual.Id, usuarioActual.Nombre);
                }

                MessageBox.Show("✓ Contraseña cambiada exitosamente", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpiar campos
                txtPasswordActual.Clear();
                txtPasswordNueva.Clear();
                txtPasswordConfirmar.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cambiar contraseña: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
