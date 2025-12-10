using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TuTiendita
{
    public partial class DialogoMetodoPago : Window
    {
        public string MetodoPagoSeleccionado { get; private set; }

        public DialogoMetodoPago()
        {
            InitializeComponent();
            ActualizarEstilos();
        }

        private void BrdEfectivo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbEfectivo.IsChecked = true;
            ActualizarEstilos();
        }

        private void BrdTarjeta_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbTarjeta.IsChecked = true;
            ActualizarEstilos();
        }

        private void BrdTransferencia_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbTransferencia.IsChecked = true;
            ActualizarEstilos();
        }

        private void ActualizarEstilos()
        {
            var colorSeleccionado = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            var colorNoSeleccionado = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            var fondoSeleccionado = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F8F5"));
            var fondoNoSeleccionado = new SolidColorBrush(Colors.White);

            // Efectivo
            if (rbEfectivo.IsChecked == true)
            {
                brdEfectivo.BorderBrush = colorSeleccionado;
                brdEfectivo.BorderThickness = new Thickness(2);
                brdEfectivo.Background = fondoSeleccionado;
            }
            else
            {
                brdEfectivo.BorderBrush = colorNoSeleccionado;
                brdEfectivo.BorderThickness = new Thickness(1);
                brdEfectivo.Background = fondoNoSeleccionado;
            }

            // Tarjeta
            if (rbTarjeta.IsChecked == true)
            {
                brdTarjeta.BorderBrush = colorSeleccionado;
                brdTarjeta.BorderThickness = new Thickness(2);
                brdTarjeta.Background = fondoSeleccionado;
            }
            else
            {
                brdTarjeta.BorderBrush = colorNoSeleccionado;
                brdTarjeta.BorderThickness = new Thickness(1);
                brdTarjeta.Background = fondoNoSeleccionado;
            }

            // Transferencia
            if (rbTransferencia.IsChecked == true)
            {
                brdTransferencia.BorderBrush = colorSeleccionado;
                brdTransferencia.BorderThickness = new Thickness(2);
                brdTransferencia.Background = fondoSeleccionado;
            }
            else
            {
                brdTransferencia.BorderBrush = colorNoSeleccionado;
                brdTransferencia.BorderThickness = new Thickness(1);
                brdTransferencia.Background = fondoNoSeleccionado;
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (rbEfectivo.IsChecked == true)
            {
                MetodoPagoSeleccionado = "Efectivo";
            }
            else if (rbTarjeta.IsChecked == true)
            {
                MetodoPagoSeleccionado = "Tarjeta";
            }
            else if (rbTransferencia.IsChecked == true)
            {
                MetodoPagoSeleccionado = "Transferencia";
            }
            else
            {
                MessageBox.Show("Debe seleccionar un metodo de pago.", "Validacion",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
