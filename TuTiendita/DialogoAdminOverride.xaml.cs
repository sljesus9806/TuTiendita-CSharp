using System;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Input;

namespace TuTiendita
{
    public partial class DialogoAdminOverride : Window
    {
        public Usuario AdminAutenticado { get; private set; }

        public DialogoAdminOverride()
        {
            InitializeComponent();
            txtUsuario.Focus();
        }

        private void BtnAutenticar_Click(object sender, RoutedEventArgs e)
        {
            AutenticarAdmin();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtContrasena_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AutenticarAdmin();
            }
        }

        private void AutenticarAdmin()
        {
            string usuario = txtUsuario.Text.Trim();
            string contrasena = txtContrasena.Password;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                MostrarError("Por favor ingrese usuario y contraseña.");
                return;
            }

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT IdUsuario, Nombre, Contrasena, NivelAcceso FROM Usuarios WHERE Nombre = @nombre";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@nombre", usuario);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string contrasenaDB = reader.GetString(2);
                                string nivelAcceso = reader.GetString(3);

                                // Verificar contraseña
                                if (contrasena == contrasenaDB)
                                {
                                    // Verificar que sea Administrador o Gerente
                                    if (nivelAcceso == "Administrador" || nivelAcceso == "Gerente")
                                    {
                                        AdminAutenticado = new Usuario
                                        {
                                            IdUsuario = reader.GetInt32(0),
                                            Nombre = reader.GetString(1),
                                            Contrasena = contrasenaDB,
                                            NivelAcceso = nivelAcceso
                                        };

                                        DialogResult = true;
                                        Close();
                                    }
                                    else
                                    {
                                        MostrarError($"Usuario '{usuario}' no tiene permisos de administrador.\nNivel de acceso: {nivelAcceso}");
                                    }
                                }
                                else
                                {
                                    MostrarError("Contraseña incorrecta.");
                                }
                            }
                            else
                            {
                                MostrarError($"Usuario '{usuario}' no encontrado.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al autenticar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
