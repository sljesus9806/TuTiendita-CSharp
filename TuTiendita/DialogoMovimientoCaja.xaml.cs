using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace TuTiendita
{
    public partial class DialogoMovimientoCaja : Window
    {
        public string TipoMovimiento { get; private set; }
        public decimal Monto { get; private set; }
        public string Concepto { get; private set; }

        private static readonly Regex _regexNumeros = new Regex("[^0-9.]+");

        public DialogoMovimientoCaja()
        {
            InitializeComponent();
            ActualizarEstiloTipos();

            // Dar foco al campo de monto al abrir
            Loaded += (s, e) => txtMonto.Focus();
        }

        private void ActualizarEstiloTipos()
        {
            // Resetear todos los bordes
            brdGasto.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            brdGasto.BorderThickness = new Thickness(1);
            brdRetiro.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            brdRetiro.BorderThickness = new Thickness(1);
            brdDeposito.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD"));
            brdDeposito.BorderThickness = new Thickness(1);

            // Resaltar el seleccionado
            if (rbGasto.IsChecked == true)
            {
                brdGasto.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                brdGasto.BorderThickness = new Thickness(2);
            }
            else if (rbRetiro.IsChecked == true)
            {
                brdRetiro.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                brdRetiro.BorderThickness = new Thickness(2);
            }
            else if (rbDeposito.IsChecked == true)
            {
                brdDeposito.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                brdDeposito.BorderThickness = new Thickness(2);
            }
        }

        private void BrdGasto_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbGasto.IsChecked = true;
            ActualizarEstiloTipos();
        }

        private void BrdRetiro_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbRetiro.IsChecked = true;
            ActualizarEstiloTipos();
        }

        private void BrdDeposito_MouseDown(object sender, MouseButtonEventArgs e)
        {
            rbDeposito.IsChecked = true;
            ActualizarEstiloTipos();
        }

        private void TxtMonto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regexNumeros.IsMatch(e.Text);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validar monto
            if (string.IsNullOrWhiteSpace(txtMonto.Text))
            {
                MessageBox.Show("Ingrese el monto del movimiento.", "Advertencia",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtMonto.Text, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor a cero.", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validar concepto
            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                MessageBox.Show("Ingrese el concepto o descripción del movimiento.", "Advertencia",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Determinar tipo
            if (rbGasto.IsChecked == true)
                TipoMovimiento = "Gasto";
            else if (rbRetiro.IsChecked == true)
                TipoMovimiento = "Retiro";
            else if (rbDeposito.IsChecked == true)
                TipoMovimiento = "Depósito";

            Monto = monto;
            Concepto = txtConcepto.Text.Trim();

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
