using System.Windows;
using System.Windows.Controls;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para seleccionar el motivo de cancelación de un CFDI
    /// </summary>
    public partial class VentanaMotivoCancelacion : Window
    {
        public string MotivoCancelacion { get; private set; } = "02";
        public string FolioSustitucion { get; private set; } = "";

        public VentanaMotivoCancelacion()
        {
            InitializeComponent();
        }

        private void cmbMotivo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMotivo.SelectedItem is ComboBoxItem item)
            {
                string motivo = item.Tag?.ToString() ?? "02";

                // Motivo 01 requiere folio de sustitución
                if (pnlFolioSustitucion != null)
                {
                    pnlFolioSustitucion.Visibility = motivo == "01"
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        private void btnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbMotivo.SelectedItem is ComboBoxItem item)
            {
                MotivoCancelacion = item.Tag?.ToString() ?? "02";
            }

            // Validar folio de sustitución si es motivo 01
            if (MotivoCancelacion == "01")
            {
                FolioSustitucion = txtFolioSustitucion.Text.Trim();

                if (string.IsNullOrEmpty(FolioSustitucion))
                {
                    MessageBox.Show(
                        "El motivo 01 requiere el UUID del CFDI que sustituye al cancelado.",
                        "UUID Requerido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Validar formato UUID
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                    FolioSustitucion,
                    @"^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$"))
                {
                    MessageBox.Show(
                        "El formato del UUID no es válido.\n\n" +
                        "Formato esperado: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
                        "Formato Inválido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
