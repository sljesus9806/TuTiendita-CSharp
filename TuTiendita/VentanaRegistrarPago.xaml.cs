using System;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaRegistrarPago : Window
    {
        private CreditoCliente creditoActual;
        private Usuario usuarioActual;

        public VentanaRegistrarPago(CreditoCliente credito, Usuario usuario)
        {
            InitializeComponent();
            creditoActual = credito;
            usuarioActual = usuario;

            CargarDatos();
        }

        private void CargarDatos()
        {
            // Validar que los controles estén inicializados
            if (txtCreditoId == null || txtMontoTotal == null ||
                txtMontoAbonado == null || txtMontoPendiente == null)
                return;

            txtCreditoId.Text = creditoActual.Id.ToString();
            txtMontoTotal.Text = creditoActual.MontoTotalFormateado;
            txtMontoAbonado.Text = creditoActual.MontoAbonadoFormateado;
            txtMontoPendiente.Text = creditoActual.MontoPendienteFormateado;
        }

        private void TxtMonto_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Validar que los controles estén inicializados
            if (txtMonto == null || txtAviso == null)
                return;

            if (decimal.TryParse(txtMonto.Text, out decimal monto))
            {
                if (monto > creditoActual.MontoPendiente)
                {
                    txtAviso.Text = "⚠️ El monto excede el saldo pendiente. Se registrará como pago completo.";
                    txtAviso.Visibility = Visibility.Visible;
                }
                else
                {
                    txtAviso.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                txtAviso.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar monto
            if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a 0.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMonto.Focus();
                return;
            }

            // Validar que el monto no exceda el pendiente (con margen de error)
            if (monto > creditoActual.MontoPendiente + 0.01m)
            {
                var resultado = MessageBox.Show(
                    $"El monto ingresado ({monto:C}) excede el saldo pendiente ({creditoActual.MontoPendiente:C}).\n\n" +
                    $"¿Desea registrar el pago por el saldo pendiente completo?",
                    "Confirmación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    monto = creditoActual.MontoPendiente;
                }
                else
                {
                    return;
                }
            }

            // Obtener método de pago
            string metodoPago = ((ComboBoxItem)cmbMetodoPago.SelectedItem).Content.ToString();

            try
            {
                bool exito = ClientesHelper.RegistrarPagoCredito(
                    creditoActual.Id,
                    monto,
                    metodoPago,
                    usuarioActual,
                    txtNotas.Text.Trim());

                if (exito)
                {
                    MessageBox.Show(
                        $"Pago de {monto:C} registrado correctamente.\n\n" +
                        $"Nuevo saldo pendiente: {(creditoActual.MontoPendiente - monto):C}",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error al registrar el pago.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al registrar el pago: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
