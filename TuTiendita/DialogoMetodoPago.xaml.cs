using System.Windows;

namespace TuTiendita
{
    public partial class DialogoMetodoPago : Window
    {
        public string MetodoPagoSeleccionado { get; private set; }

        public DialogoMetodoPago()
        {
            InitializeComponent();
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
                // No se selecciono ningun metodo de pago
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
