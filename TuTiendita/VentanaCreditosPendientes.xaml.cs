using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaCreditosPendientes : Window
    {
        private List<OrdenCompra> ordenesCredito;

        public VentanaCreditosPendientes()
        {
            InitializeComponent();
            CargarCreditos();
        }

        private void CargarCreditos()
        {
            try
            {
                ordenesCredito = ProveedoresHelper.ObtenerOrdenesCreditoPendiente();
                dgCreditos.ItemsSource = ordenesCredito;

                // Calcular estadísticas
                decimal totalPendiente = ordenesCredito.Sum(o => o.Total);
                int ordenesVencidas = ordenesCredito.Count(o => o.DiasRestantes == 0);
                int proximasVencer = ordenesCredito.Count(o => o.DiasRestantes > 0 && o.DiasRestantes <= 3);

                txtTotalPendiente.Text = totalPendiente.ToString("C");
                txtOrdenesVencidas.Text = ordenesVencidas.ToString();
                txtProximasVencer.Text = proximasVencer.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar créditos pendientes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarCreditos();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
