using System.Data.SQLite;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TuTiendita
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Ejecutar migración de passwords al inicio (si es necesario)
            try
            {
                Helpers.PasswordMigration.EjecutarMigracionConFeedback(silencioso: true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en migración de passwords: {ex.Message}");
            }

            // Ejecutar backup automático si está configurado
            try
            {
                Helpers.BackupManager.CrearBackupAutomaticoSiNecesario();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en backup automático: {ex.Message}");
            }
        }

        private void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string nombreUsuario = txtNombreUsuario.Text;
            string contrasena = txtContrasena.Password;

            // Verificar las credenciales en la base de datos
            Usuario usuarioLogueado = VerificarCredenciales(nombreUsuario, contrasena);

            if (usuarioLogueado != null)
            {
                // Usuario y contraseña correctos - abrir ventana principal directamente (sin MessageBox)
                VentanaPrincipal ventanaPrincipal = new VentanaPrincipal(usuarioLogueado);
                ventanaPrincipal.Show();

                // Cerrar la ventana de inicio de sesión
                this.Close();
            }
            else
            {
                // Usuario o contraseña incorrectos
                // Registrar intento fallido en auditoría
                try
                {
                    Helpers.AuditLogger.RegistrarLoginFallido(nombreUsuario);
                }
                catch { }

                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Usuario VerificarCredenciales(string nombreUsuario, string contrasena)
        {
            Usuario usuario = null;

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                // Buscar usuario por nombre (no verificamos password en el query)
                string query = "SELECT * FROM Usuarios WHERE Nombre = @Nombre";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreUsuario);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string passwordHash = reader["Contrasena"].ToString();

                            // Verificar contraseña usando BCrypt (compatible con texto plano durante migración)
                            if (Helpers.SecurityHelper.VerifyPasswordCompat(contrasena, passwordHash, out bool esHash))
                            {
                                usuario = new Usuario
                                {
                                    IdUsuario = Convert.ToInt32(reader["Id"]),
                                    Nombre = reader["Nombre"].ToString(),
                                    Contrasena = passwordHash,
                                    NivelAcceso = reader["NivelAcceso"].ToString()
                                };

                                // Registrar login en auditoría
                                try
                                {
                                    Helpers.AuditLogger.RegistrarLogin(usuario);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }

            return usuario;
        }

        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnIniciarSesion_Click(this, new RoutedEventArgs());
            }
        }
    }
}