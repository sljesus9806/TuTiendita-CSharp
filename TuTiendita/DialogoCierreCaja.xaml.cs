using System;
using System.Windows;
using System.Windows.Controls;

namespace TuTiendita
{
    public partial class DialogoCierreCaja : Window
    {
        public decimal MontoFinalContado { get; private set; }
        public string NotasCierre { get; private set; }

        public DialogoCierreCaja()
        {
            InitializeComponent();
        }

        private void Denominacion_Changed(object sender, TextChangedEventArgs e)
        {
            CalcularTotales();
        }

        private void CalcularTotales()
        {
            try
            {
                decimal total = 0;

                // Billetes
                if (int.TryParse(txt1000.Text, out int cant1000))
                {
                    decimal subtotal = cant1000 * 1000;
                    total1000.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt500.Text, out int cant500))
                {
                    decimal subtotal = cant500 * 500;
                    total500.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt200.Text, out int cant200))
                {
                    decimal subtotal = cant200 * 200;
                    total200.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt100.Text, out int cant100))
                {
                    decimal subtotal = cant100 * 100;
                    total100.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt50.Text, out int cant50))
                {
                    decimal subtotal = cant50 * 50;
                    total50.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt20.Text, out int cant20))
                {
                    decimal subtotal = cant20 * 20;
                    total20.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                // Monedas
                if (int.TryParse(txt10.Text, out int cant10))
                {
                    decimal subtotal = cant10 * 10;
                    total10.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt5.Text, out int cant5))
                {
                    decimal subtotal = cant5 * 5;
                    total5.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt2.Text, out int cant2))
                {
                    decimal subtotal = cant2 * 2;
                    total2.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                if (int.TryParse(txt1.Text, out int cant1))
                {
                    decimal subtotal = cant1 * 1;
                    total1.Text = subtotal.ToString("C");
                    total += subtotal;
                }

                txtTotalGeneral.Text = total.ToString("C");
            }
            catch
            {
                // Ignorar errores de conversión
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            CalcularTotales();

            try
            {
                // Limpiar el texto para el parseo: remover símbolo de moneda, comas y espacios
                string textoLimpio = txtTotalGeneral.Text
                    .Replace("$", "")
                    .Replace(",", "")
                    .Replace(" ", "")
                    .Trim();

                if (decimal.TryParse(textoLimpio, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out decimal monto))
                {
                    MontoFinalContado = monto;
                    NotasCierre = txtNotas.Text ?? "";
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Error al calcular el total. Verifique los valores ingresados.",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar el cierre: {ex.Message}\n\nVerifique que todos los valores sean numéricos.",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
