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
