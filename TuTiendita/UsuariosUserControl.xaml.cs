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
            ActualizarResumen();
        }

        private void ActualizarResumen()
        {
            // Actualizar las tarjetas de resumen
            txtTotalUsuarios.Text = usuarios.Count.ToString();
            txtTotalGerentes.Text = usuarios.Count(u => u.NivelAcceso == "Gerente").ToString();
            txtTotalCajeros.Text = usuarios.Count(u => u.NivelAcceso == "Cajero").ToString();
        }

        private void VerificarPermisos()
        {
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                // Si no es gerente, deshabilita la sección de gestión de usuarios
                txtNombre.IsEnabled = false;
                txtContrasena.IsEnabled = false;
                cmbNivelAcceso.IsEnabled = false;
                btnAgregarUsuario.IsEnabled = false;
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
                MessageBox.Show("Usuario agregado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarUsuarios(); // Recargar para obtener el ID correcto de la BD
                LimpiarFormulario();
            }
            else
            {
                MessageBox.Show("Solo un gerente puede agregar otro gerente.", "Permiso denegado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el ID del usuario desde el Tag del botón
            var button = sender as Button;
            if (button == null) return;

            int idUsuario = Convert.ToInt32(button.Tag);
            var usuarioSeleccionado = usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Usuario no encontrado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Crear un diálogo para editar
            var resultado = MessageBox.Show($"¿Desea editar el usuario '{usuarioSeleccionado.Nombre}'?",
                "Editar Usuario", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                // Prellenar los campos con los datos actuales
                txtNombre.Text = usuarioSeleccionado.Nombre;
                txtContrasena.Password = usuarioSeleccionado.Contrasena;

                // Seleccionar el nivel de acceso correspondiente
                foreach (ComboBoxItem item in cmbNivelAcceso.Items)
                {
                    if (item.Content.ToString() == usuarioSeleccionado.NivelAcceso)
                    {
                        cmbNivelAcceso.SelectedItem = item;
                        break;
                    }
                }

                // Mostrar mensaje para que el usuario actualice
                MessageBox.Show("Modifique los datos y presione 'Agregar Usuario' para actualizar.",
                    "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el ID del usuario desde el Tag del botón
            var button = sender as Button;
            if (button == null) return;

            int idUsuario = Convert.ToInt32(button.Tag);
            var usuarioSeleccionado = usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Usuario no encontrado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Verificar que no se elimine el último gerente
            if (usuarioSeleccionado.NivelAcceso == "Gerente" && usuarios.Count(u => u.NivelAcceso == "Gerente") == 1)
            {
                MessageBox.Show("No se puede eliminar el único gerente del sistema.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Confirmar eliminación
            var resultado = MessageBox.Show($"¿Está seguro de eliminar el usuario '{usuarioSeleccionado.Nombre}'?",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
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

                MessageBox.Show("Usuario eliminado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarUsuarios();
            }
        }

        private void ActualizarDataGrid()
        {
            dgUsuarios.ItemsSource = null;
            dgUsuarios.ItemsSource = usuarios;
            ActualizarResumen();
        }

        private void LimpiarFormulario()
        {
            txtNombre.Clear();
            txtContrasena.Clear();
            cmbNivelAcceso.SelectedIndex = -1;
        }
    }
}
