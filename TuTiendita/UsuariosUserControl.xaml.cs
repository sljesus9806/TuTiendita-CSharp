using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TuTiendita
{
    /// <summary>
    /// Interaction logic for UsuariosUserControl.xaml
    /// </summary>
    public partial class UsuariosUserControl : UserControl
    {
        private List<Usuario> usuarios; // Lista que almacena los usuarios
        private Usuario usuarioActual; // El usuario que está utilizando la aplicación

        public UsuariosUserControl(Usuario usuarioLogueado)
        {
            InitializeComponent();
            usuarioActual = usuarioLogueado;
            CargarUsuarios();
            VerificarPermisos();
        }

        private void CargarUsuarios()
        {
            // Cargar usuarios desde la base de datos
            using (var connection = Database.GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Usuarios";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        usuarios = new List<Usuario>();
                        while (reader.Read())
                        {
                            usuarios.Add(new Usuario
                            {
                                IdUsuario = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"].ToString(),
                                Contrasena = reader["Contrasena"].ToString(),
                                NivelAcceso = reader["NivelAcceso"].ToString()
                            });
                        }
                    }
                }
            }
            dgUsuarios.ItemsSource = usuarios;
        }

        private void VerificarPermisos()
        {
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                // Si no es gerente, oculta la sección de gestión de usuarios
                btnAgregarUsuario.Visibility = Visibility.Collapsed;
                btnEditarUsuario.Visibility = Visibility.Collapsed;
                btnEliminarUsuario.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (cmbNivelAcceso.SelectedItem == null || txtNombre.Text == string.Empty || txtContrasena.Password == string.Empty)
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string nivelSeleccionado = (cmbNivelAcceso.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (usuarioActual.NivelAcceso == "Gerente" || nivelSeleccionado != "Gerente")
            {
                var nuevoUsuario = new Usuario
                {
                    IdUsuario = usuarios.Any() ? usuarios.Max(u => u.IdUsuario) + 1 : 1, // Simulación de autoincremento de ID
                    Nombre = txtNombre.Text,
                    Contrasena = txtContrasena.Password,
                    NivelAcceso = nivelSeleccionado
                };

                // Agregar a la base de datos
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO Usuarios (Nombre, Contrasena, NivelAcceso) VALUES (@Nombre, @Contrasena, @NivelAcceso)";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", nuevoUsuario.Nombre);
                        cmd.Parameters.AddWithValue("@Contrasena", nuevoUsuario.Contrasena);
                        cmd.Parameters.AddWithValue("@NivelAcceso", nuevoUsuario.NivelAcceso);
                        cmd.ExecuteNonQuery();
                    }
                }

                usuarios.Add(nuevoUsuario);
                ActualizarDataGrid();
                LimpiarFormulario();
            }
            else
            {
                MessageBox.Show("Solo un gerente puede agregar otro gerente.", "Permiso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un usuario para editar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var usuarioSeleccionado = dgUsuarios.SelectedItem as Usuario;

            if (usuarioSeleccionado != null)
            {
                usuarioSeleccionado.Nombre = txtNombre.Text;
                usuarioSeleccionado.Contrasena = txtContrasena.Password;
                usuarioSeleccionado.NivelAcceso = (cmbNivelAcceso.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Actualizar en la base de datos
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE Usuarios SET Nombre = @Nombre, Contrasena = @Contrasena, NivelAcceso = @NivelAcceso WHERE Id = @Id";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioSeleccionado.IdUsuario);
                        cmd.Parameters.AddWithValue("@Nombre", usuarioSeleccionado.Nombre);
                        cmd.Parameters.AddWithValue("@Contrasena", usuarioSeleccionado.Contrasena);
                        cmd.Parameters.AddWithValue("@NivelAcceso", usuarioSeleccionado.NivelAcceso);
                        cmd.ExecuteNonQuery();
                    }
                }

                ActualizarDataGrid();
                LimpiarFormulario();
            }
        }

        private void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un usuario para eliminar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var usuarioSeleccionado = dgUsuarios.SelectedItem as Usuario;

            if (usuarioSeleccionado != null)
            {
                // Eliminar de la base de datos
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM Usuarios WHERE Id = @Id";
                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", usuarioSeleccionado.IdUsuario);
                        cmd.ExecuteNonQuery();
                    }
                }

                usuarios.Remove(usuarioSeleccionado);
                ActualizarDataGrid();
                LimpiarFormulario();
            }
        }

        private void ActualizarDataGrid()
        {
            dgUsuarios.ItemsSource = null;
            dgUsuarios.ItemsSource = usuarios;
        }

        private void LimpiarFormulario()
        {
            txtNombre.Clear();
            txtContrasena.Clear();
            cmbNivelAcceso.SelectedIndex = -1;
        }
    }
}
