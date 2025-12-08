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
using TuTiendita.Helpers;

namespace TuTiendita
{
    /// <summary>
    /// Interaction logic for UsuariosUserControl.xaml
    /// </summary>
    public partial class UsuariosUserControl : UserControl
    {
        private List<Usuario> usuarios; // Lista que almacena los usuarios
        private Usuario usuarioActual; // El usuario que esta utilizando la aplicacion
        private int? usuarioEditandoId = null; // ID del usuario que se esta editando (null si es nuevo)

        public UsuariosUserControl(Usuario usuarioLogueado)
        {
            InitializeComponent();
            usuarioActual = usuarioLogueado;
            CargarUsuarios();
            VerificarPermisos();
        }

        private void CargarUsuarios()
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar usuarios: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                // Si no es gerente, deshabilita la seccion de gestion de usuarios
                txtNombre.IsEnabled = false;
                txtContrasena.IsEnabled = false;
                cmbNivelAcceso.IsEnabled = false;
                btnAgregarUsuario.IsEnabled = false;
            }
        }

        private bool ValidarNombreUsuario(string nombre, int? excluirId = null)
        {
            // Verificar si el nombre de usuario ya existe
            var usuarioExistente = usuarios.FirstOrDefault(u =>
                u.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                (!excluirId.HasValue || u.IdUsuario != excluirId.Value));

            return usuarioExistente == null;
        }

        private void BtnAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("El nombre de usuario es obligatorio.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtContrasena.Password))
            {
                // Si estamos editando y no se ingresa contrasena, mantener la actual
                if (!usuarioEditandoId.HasValue)
                {
                    MessageBox.Show("La contrasena es obligatoria.", "Validacion",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtContrasena.Focus();
                    return;
                }
            }

            if (cmbNivelAcceso.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un nivel de acceso.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbNivelAcceso.Focus();
                return;
            }

            string nombreUsuario = txtNombre.Text.Trim();
            string nivelSeleccionado = (cmbNivelAcceso.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Validar longitud de contrasena
            if (!string.IsNullOrWhiteSpace(txtContrasena.Password) && txtContrasena.Password.Length < 4)
            {
                MessageBox.Show("La contrasena debe tener al menos 4 caracteres.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContrasena.Focus();
                return;
            }

            // Verificar permisos para crear gerentes
            if (nivelSeleccionado == "Gerente" && usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo un gerente puede crear otro gerente.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AuditLogger.RegistrarPermisosDenegados(usuarioActual, "Crear usuario gerente");
                return;
            }

            try
            {
                if (usuarioEditandoId.HasValue)
                {
                    // MODO EDICION
                    var usuarioEditando = usuarios.FirstOrDefault(u => u.IdUsuario == usuarioEditandoId.Value);
                    if (usuarioEditando == null)
                    {
                        MessageBox.Show("Usuario no encontrado.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        LimpiarFormulario();
                        return;
                    }

                    // Verificar nombre duplicado (excluyendo el usuario actual)
                    if (!ValidarNombreUsuario(nombreUsuario, usuarioEditandoId))
                    {
                        MessageBox.Show($"Ya existe otro usuario con el nombre '{nombreUsuario}'.",
                            "Nombre Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtNombre.Focus();
                        return;
                    }

                    // Verificar si esta cambiando el rol del ultimo gerente
                    if (usuarioEditando.NivelAcceso == "Gerente" && nivelSeleccionado != "Gerente")
                    {
                        if (usuarios.Count(u => u.NivelAcceso == "Gerente") == 1)
                        {
                            MessageBox.Show("No se puede cambiar el rol del unico gerente del sistema.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string rolAnterior = usuarioEditando.NivelAcceso;

                    // Actualizar en la base de datos
                    using (var connection = Database.GetConnection())
                    {
                        connection.Open();
                        string query;
                        SQLiteCommand cmd;

                        if (!string.IsNullOrWhiteSpace(txtContrasena.Password))
                        {
                            // Actualizar con nueva contrasena (hasheada)
                            string contrasenaHash = SecurityHelper.HashPassword(txtContrasena.Password);
                            query = "UPDATE Usuarios SET Nombre = @Nombre, Contrasena = @Contrasena, NivelAcceso = @NivelAcceso WHERE Id = @Id";
                            cmd = new SQLiteCommand(query, connection);
                            cmd.Parameters.AddWithValue("@Contrasena", contrasenaHash);
                        }
                        else
                        {
                            // Actualizar sin cambiar contrasena
                            query = "UPDATE Usuarios SET Nombre = @Nombre, NivelAcceso = @NivelAcceso WHERE Id = @Id";
                            cmd = new SQLiteCommand(query, connection);
                        }

                        cmd.Parameters.AddWithValue("@Nombre", nombreUsuario);
                        cmd.Parameters.AddWithValue("@NivelAcceso", nivelSeleccionado);
                        cmd.Parameters.AddWithValue("@Id", usuarioEditandoId.Value);
                        cmd.ExecuteNonQuery();
                    }

                    // Auditoría
                    if (rolAnterior != nivelSeleccionado)
                    {
                        AuditLogger.RegistrarCambioRol(usuarioActual, usuarioEditandoId.Value, nombreUsuario, rolAnterior, nivelSeleccionado);
                    }

                    MessageBox.Show("Usuario actualizado correctamente.", "Exito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // MODO NUEVO USUARIO

                    // Verificar nombre duplicado
                    if (!ValidarNombreUsuario(nombreUsuario))
                    {
                        MessageBox.Show($"Ya existe un usuario con el nombre '{nombreUsuario}'.",
                            "Nombre Duplicado", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtNombre.Focus();
                        return;
                    }

                    // Hashear la contrasena
                    string contrasenaHash = SecurityHelper.HashPassword(txtContrasena.Password);

                    // Agregar a la base de datos
                    int nuevoId;
                    using (var connection = Database.GetConnection())
                    {
                        connection.Open();
                        string query = "INSERT INTO Usuarios (Nombre, Contrasena, NivelAcceso) VALUES (@Nombre, @Contrasena, @NivelAcceso); SELECT last_insert_rowid();";
                        using (var cmd = new SQLiteCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", nombreUsuario);
                            cmd.Parameters.AddWithValue("@Contrasena", contrasenaHash);
                            cmd.Parameters.AddWithValue("@NivelAcceso", nivelSeleccionado);
                            nuevoId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }

                    // Auditoria
                    AuditLogger.RegistrarUsuarioCreado(usuarioActual, nuevoId, nombreUsuario, nivelSeleccionado);

                    MessageBox.Show("Usuario agregado correctamente.", "Exito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CargarUsuarios();
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar usuario: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden editar usuarios.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Obtener el ID del usuario desde el Tag del boton
            var button = sender as Button;
            if (button == null) return;

            int idUsuario = Convert.ToInt32(button.Tag);
            var usuarioSeleccionado = usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Usuario no encontrado.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Entrar en modo edicion
            usuarioEditandoId = idUsuario;

            // Prellenar los campos con los datos actuales
            txtNombre.Text = usuarioSeleccionado.Nombre;
            txtContrasena.Password = ""; // No mostrar la contrasena hasheada

            // Seleccionar el nivel de acceso correspondiente
            foreach (ComboBoxItem item in cmbNivelAcceso.Items)
            {
                if (item.Content.ToString() == usuarioSeleccionado.NivelAcceso)
                {
                    cmbNivelAcceso.SelectedItem = item;
                    break;
                }
            }

            // Cambiar el texto del boton para indicar modo edicion
            btnAgregarUsuario.Content = "Actualizar Usuario";

            MessageBox.Show($"Editando usuario: {usuarioSeleccionado.Nombre}\n\n" +
                "Modifique los datos y presione 'Actualizar Usuario'.\n" +
                "Deje la contrasena vacia para mantener la actual.",
                "Modo Edicion", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Verificar permisos
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                MessageBox.Show("Solo los gerentes pueden eliminar usuarios.", "Permiso Denegado",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Obtener el ID del usuario desde el Tag del boton
            var button = sender as Button;
            if (button == null) return;

            int idUsuario = Convert.ToInt32(button.Tag);
            var usuarioSeleccionado = usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Usuario no encontrado.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Verificar que no se elimine a si mismo
            if (usuarioSeleccionado.IdUsuario == usuarioActual.IdUsuario)
            {
                MessageBox.Show("No puede eliminarse a si mismo.\nPida a otro gerente que realice esta accion.",
                    "Operacion No Permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verificar que no se elimine el ultimo gerente
            if (usuarioSeleccionado.NivelAcceso == "Gerente" && usuarios.Count(u => u.NivelAcceso == "Gerente") == 1)
            {
                MessageBox.Show("No se puede eliminar el unico gerente del sistema.\n" +
                    "Primero cree otro usuario con rol de Gerente.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Confirmar eliminacion
            var resultado = MessageBox.Show(
                $"Esta seguro de eliminar el usuario '{usuarioSeleccionado.Nombre}'?\n\n" +
                "Esta accion no se puede deshacer.",
                "Confirmar Eliminacion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
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

                    // Auditoria
                    AuditLogger.RegistrarUsuarioEliminado(usuarioActual, usuarioSeleccionado.IdUsuario, usuarioSeleccionado.Nombre);

                    MessageBox.Show("Usuario eliminado correctamente.", "Exito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar usuario: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            usuarioEditandoId = null;
            btnAgregarUsuario.Content = "Agregar Usuario";
        }
    }
}
