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
    /// <summary>
    /// Interaction logic for VentanaPrincipal.xaml
    /// </summary>
    public partial class VentanaPrincipal : Window
    {
        public VentanaPrincipal()
        {
            InitializeComponent();
        }
        
        private void Ventas_Click(object sender, RoutedEventArgs e)
        {
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
        }

        private void Configuracion_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Migracion_Click(object sender, RoutedEventArgs e)
        {
        }



    }
}
