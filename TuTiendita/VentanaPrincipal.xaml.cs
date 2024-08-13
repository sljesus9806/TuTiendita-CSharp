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

        // Constructor que recibe el usuario logueado
        public VentanaPrincipal(Usuario usuario)
        {
            InitializeComponent();
            usuarioActual = usuario;
            ConfigurarInterfazSegunUsuario();
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
            MainContent.Content = new VentasUserControl();
        }

        private void CerrarCaja_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Productos_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ProductosUserControl();
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

        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Migracion_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}