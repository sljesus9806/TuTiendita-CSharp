using System;
using System.Collections.Generic;
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
    public partial class VentanaPrincipal : Window
    {
        private Usuario usuarioActual;
        private Usuario usuarioOriginal; // Guarda el usuario original (cajero) durante Admin Override

        // Constructor que recibe el usuario logueado
        public VentanaPrincipal(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            usuarioOriginal = null; // No hay override inicial
            ConfigurarInterfazSegunUsuario();
            ActualizarInfoUsuario();
        }

        private void ActualizarInfoUsuario()
        {
            if (usuarioActual != null)
            {
                txtUsuarioActual.Text = usuarioActual.Nombre;
                txtRolActual.Text = usuarioActual.NivelAcceso;

                // Cambiar color según el rol
                if (usuarioActual.NivelAcceso == "Cajero")
                {
                    txtRolActual.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3498DB"));
                }
                else if (usuarioActual.NivelAcceso == "Administrador" || usuarioActual.NivelAcceso == "Gerente")
                {
                    txtRolActual.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E67E22"));
                }

                // Si hay un override activo, mostrar indicador
                if (usuarioOriginal != null)
                {
                    txtUsuarioActual.Text = $"{usuarioActual.Nombre} (Override de: {usuarioOriginal.Nombre})";
                    btnCambiarUsuario.Content = "🔓 Devolver Control a " + usuarioOriginal.Nombre;
                    btnCambiarUsuario.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
                }
                else
                {
                    btnCambiarUsuario.Content = "🔐 Cambiar Usuario (Admin)";
                    btnCambiarUsuario.Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E67E22"));
                }
            }
        }

        // Método para configurar la interfaz según el nivel de acceso del usuario
        private void ConfigurarInterfazSegunUsuario()
        {
            // Ejemplo: Ocultar el botón "Usuarios" si el usuario no es un gerente
            if (usuarioActual.NivelAcceso != "Gerente")
            {
                Usuarios.Visibility = Visibility.Collapsed;
            }

            // Puedes agregar más lógica para configurar la interfaz según sea necesario
        }

        private void Ventas_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new VentasUserControl(usuarioActual);
        }

        private void CerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new CerrarCajaUserControl(usuarioActual);
        }

        private void Productos_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ProductosUserControl(usuarioActual);
        }

        private void Compras_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Clientes_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Usuarios_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new UsuariosUserControl(usuarioActual);
        }

        private void Reportes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReportesUserControl();
        }

        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
        }

        private void BtnCambiarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Si hay un override activo, devolver control al usuario original
            if (usuarioOriginal != null)
            {
                DevolverControlUsuarioOriginal();
                return;
            }

            // Si no hay override, solicitar autenticación de admin
            var dialogoAdmin = new DialogoAdminOverride();
            if (dialogoAdmin.ShowDialog() == true)
            {
                var admin = dialogoAdmin.AdminAutenticado;

                // Guardar el usuario actual (cajero) como original
                usuarioOriginal = usuarioActual;

                // Cambiar al administrador
                usuarioActual = admin;

                // Actualizar interfaz
                ConfigurarInterfazSegunUsuario();
                ActualizarInfoUsuario();

                // Refrescar la vista actual para que use el nuevo usuario
                RefrescarVistaActual();

                MessageBox.Show($"Control transferido al administrador: {admin.Nombre}\n\n" +
                              $"Puede realizar cambios y configuraciones.\n" +
                              $"Use 'Devolver Control' para regresar al usuario original.",
                              "Cambio de Usuario Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DevolverControlUsuarioOriginal()
        {
            if (usuarioOriginal == null) return;

            // Preguntar al admin si desea cerrar el turno
            var resultado = MessageBox.Show(
                $"¿Desea cerrar el turno actual antes de devolver el control a {usuarioOriginal.Nombre}?\n\n" +
                $"• SÍ: Cerrará el turno y devolverá el control\n" +
                $"• NO: Mantendrá el turno abierto y devolverá el control\n" +
                $"• CANCELAR: Permanecerá como administrador",
                "Devolver Control al Cajero",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Cancel)
            {
                return; // El admin decide quedarse
            }

            if (resultado == MessageBoxResult.Yes)
            {
                // Redirigir a Gestión de Turno para que cierre el turno
                MessageBox.Show("Por favor cierre el turno en la sección 'Gestión de Turno'.\n\n" +
                              "Después de cerrar el turno, use el botón 'Devolver Control' nuevamente.",
                              "Cierre de Turno", MessageBoxButton.OK, MessageBoxImage.Information);
                MainContent.Content = new CerrarCajaUserControl(usuarioActual);
                return;
            }

            // Si elige NO, devolver control sin cerrar turno
            usuarioActual = usuarioOriginal;
            usuarioOriginal = null;

            ConfigurarInterfazSegunUsuario();
            ActualizarInfoUsuario();
            RefrescarVistaActual();

            MessageBox.Show($"Control devuelto a: {usuarioActual.Nombre}\n" +
                          $"El turno permanece abierto.",
                          "Control Devuelto", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefrescarVistaActual()
        {
            // Refrescar la vista actual con el nuevo usuario
            if (MainContent.Content is VentasUserControl)
            {
                MainContent.Content = new VentasUserControl(usuarioActual);
            }
            else if (MainContent.Content is CerrarCajaUserControl)
            {
                MainContent.Content = new CerrarCajaUserControl(usuarioActual);
            }
            else if (MainContent.Content is ProductosUserControl)
            {
                MainContent.Content = new ProductosUserControl(usuarioActual);
            }
            else if (MainContent.Content is UsuariosUserControl)
            {
                MainContent.Content = new UsuariosUserControl(usuarioActual);
            }
            else if (MainContent.Content is ReportesUserControl)
            {
                MainContent.Content = new ReportesUserControl();
            }
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show(
                "¿Está seguro que desea cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                // Abrir ventana de login
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}