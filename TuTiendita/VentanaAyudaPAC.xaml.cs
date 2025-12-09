using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace TuTiendita
{
    /// <summary>
    /// Ventana de ayuda con guia completa para configurar PAC y facturacion
    /// </summary>
    public partial class VentanaAyudaPAC : Window
    {
        public VentanaAyudaPAC()
        {
            InitializeComponent();
        }

        private void AbrirEnlace_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.TextBlock textBlock && textBlock.Tag is string url)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo abrir el enlace.\n\nError: {ex.Message}\n\n" +
                    "Puedes copiar la direccion y pegarla en tu navegador.",
                    "Error al Abrir Enlace",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
