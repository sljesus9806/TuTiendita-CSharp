using System;
using System.Windows;

namespace TuTiendita
{
    public partial class DialogoMovimientoCaja : Window
    {
        public string TipoMovimiento { get; private set; }
        public decimal Monto { get; private set; }
        public string Concepto { get; private set; }

        public DialogoMovimientoCaja()
        {
            InitializeComponent();
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
