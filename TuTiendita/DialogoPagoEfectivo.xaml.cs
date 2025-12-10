using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TuTiendita
{
    public partial class DialogoPagoEfectivo : Window
    {
        public decimal TotalAPagar { get; private set; }
        public decimal MontoRecibido { get; private set; }
        public decimal Cambio { get; private set; }

        private static readonly Regex _regexNumeros = new Regex("[^0-9.]+");

        public DialogoPagoEfectivo(decimal total)
        {
            InitializeComponent();
            TotalAPagar = total;
            txtTotalPagar.Text = total.ToString("C");

            // Dar foco al campo de monto
            Loaded += (s, e) =>
            {
                txtMontoRecibido.Focus();
                txtMontoRecibido.SelectAll();
            };
        }

        private void TxtMontoRecibido_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir numeros y punto decimal
            e.Handled = _regexNumeros.IsMatch(e.Text);
        }

        private void TxtMontoRecibido_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalcularCambio();
        }

        private void TxtMontoRecibido_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnConfirmar.IsEnabled)
            {
                BtnConfirmar_Click(sender, e);
            }
        }

        private void BtnMontoRapido_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                txtMontoRecibido.Text = btn.Tag.ToString();
                txtMontoRecibido.Focus();
                txtMontoRecibido.CaretIndex = txtMontoRecibido.Text.Length;
            }
        }

        private void CalcularCambio()
        {
            txtError.Visibility = Visibility.Collapsed;

            if (decimal.TryParse(txtMontoRecibido.Text, out decimal monto))
            {
                MontoRecibido = monto;
                Cambio = monto - TotalAPagar;

                if (Cambio >= 0)
                {
                    txtCambio.Text = Cambio.ToString("C");
                    txtCambio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    brdCambio.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    brdCambio.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F8F5"));
                    btnConfirmar.IsEnabled = true;
                }
                else
                {
                    decimal faltante = Math.Abs(Cambio);
                    txtCambio.Text = $"-{faltante:C}";
                    txtCambio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    brdCambio.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    brdCambio.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDEDEC"));
                    btnConfirmar.IsEnabled = false;
                    txtError.Text = $"Falta {faltante:C} para completar el pago";
                    txtError.Visibility = Visibility.Visible;
                }
            }
            else
            {
                txtCambio.Text = "$0.00";
                txtCambio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
                brdCambio.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7"));
                brdCambio.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F9FA"));
                btnConfirmar.IsEnabled = false;
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtMontoRecibido.Text, out decimal monto))
            {
                txtError.Text = "Ingrese un monto valido";
                txtError.Visibility = Visibility.Visible;
                txtMontoRecibido.Focus();
                return;
            }

            if (monto < TotalAPagar)
            {
                txtError.Text = "El monto es insuficiente";
                txtError.Visibility = Visibility.Visible;
                txtMontoRecibido.Focus();
                return;
            }

            // Validar monto razonable
            if (monto > 100000)
            {
                var result = MessageBox.Show(
                    $"El monto ingresado ({monto:C}) parece muy alto.\nDesea continuar?",
                    "Verificar Monto",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    txtMontoRecibido.Focus();
                    txtMontoRecibido.SelectAll();
                    return;
                }
            }

            MontoRecibido = monto;
            Cambio = Math.Round(monto - TotalAPagar, 2);
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
