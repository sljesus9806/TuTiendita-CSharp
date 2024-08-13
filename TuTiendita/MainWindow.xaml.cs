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
        }

        private void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)
        {
            string nombreUsuario = txtNombreUsuario.Text;
            string contrasena = txtContrasena.Password;

            // Verificar las credenciales en la base de datos
            Usuario usuarioLogueado = VerificarCredenciales(nombreUsuario, contrasena);

            if (usuarioLogueado != null)
            {
                // Usuario y contraseña correctos
                MessageBox.Show("Inicio de sesión exitoso!");

                // Crear y mostrar la ventana principal con el usuario logueado
                VentanaPrincipal ventanaPrincipal = new VentanaPrincipal(usuarioLogueado);
                ventanaPrincipal.Show();

                // Cerrar la ventana de inicio de sesión
                this.Close();
            }
            else
            {
                // Usuario o contraseña incorrectos
                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Usuario VerificarCredenciales(string nombreUsuario, string contrasena)
        {
            Usuario usuario = null;

            using (var connection = Database.GetConnection())
            {
                connection.Open();

                string query = "SELECT * FROM Usuarios WHERE Nombre = @Nombre AND Contrasena = @Contrasena";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Nombre", nombreUsuario);
                    cmd.Parameters.AddWithValue("@Contrasena", contrasena);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new Usuario
                            {
                                IdUsuario = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Contrasena = reader["Contrasena"].ToString(),
                                NivelAcceso = reader["NivelAcceso"].ToString()
                            };
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