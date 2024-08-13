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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void BtnIniciarSesion_Click(object sender, RoutedEventArgs e)

        {
            string nombreUsuario = txtNombreUsuario.Text;
            string contrasena = txtContrasena.Password;

            if (nombreUsuario == "admin" && contrasena == "1234")
            {
                // Usuario y contraseña correctos
                MessageBox.Show("Inicio de sesión exitoso!");
                txtContrasena.IsEnabled = false;
                txtNombreUsuario.IsEnabled = false;
                VentanaPrincipal ventanaPrincipal = new VentanaPrincipal();
                ventanaPrincipal.Show();
                this.Close(); // Cierra la ventana de inicio de sesión
            }
            else
            {
                // Usuario o contraseña incorrectos
                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        public void Txt_KeyDown(object sender, KeyEventArgs e)

        {

            if (e.Key == Key.Enter)
            {

                // Llama al método que maneja el clic en el botón de inicio de sesión
                BtnIniciarSesion_Click(this, new RoutedEventArgs());


            }


        }

    }

}