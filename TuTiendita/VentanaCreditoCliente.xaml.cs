using System;
using System.Linq;
using System.Windows;
using TuTiendita.Helpers;

namespace TuTiendita
{
    public partial class VentanaCreditoCliente : Window
    {
        private Cliente clienteActual;
        private Usuario usuarioActual;

        public VentanaCreditoCliente(Cliente cliente, Usuario usuario)
        {
            InitializeComponent();
            clienteActual = cliente;
            usuarioActual = usuario;

            // Validar que los controles críticos estén inicializados
            if (txtTitulo != null)
                txtTitulo.Text = $"💳 Gestión de Crédito - {cliente.NombreCompleto}";
            if (txtCliente != null)
                txtCliente.Text = $"Cliente: {cliente.NombreCompleto} | Documento: {cliente.Documento ?? "N/A"}";

            CargarDatos();
        }

        private void CargarDatos()
        {
            // Validar que los controles estén inicializados
            if (txtLimiteCredito == null || txtDeudaActual == null ||
                txtCreditoDisponible == null || dgCreditos == null ||
                dgPagos == null || txtTotalPagado == null)
                return;

            // Actualizar tarjetas de resumen
            txtLimiteCredito.Text = clienteActual.LimiteCreditoFormateado;
            txtDeudaActual.Text = clienteActual.DeudaTotalFormateada;
            txtCreditoDisponible.Text = clienteActual.CreditoDisponibleFormateado;

            // Cargar créditos activos
            var creditos = ClientesHelper.ObtenerCreditosCliente(clienteActual.Id);
            dgCreditos.ItemsSource = creditos;

            // Cargar historial de pagos
            var pagos = ClientesHelper.ObtenerPagosCliente(clienteActual.Id);
            dgPagos.ItemsSource = pagos;

            // Calcular total pagado
            decimal totalPagado = pagos.Sum(p => p.Monto);
            txtTotalPagado.Text = $"Total Pagado: {totalPagado:C}";
        }

        private void BtnAbonar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int creditoId)
            {
                var credito = ClientesHelper.ObtenerCreditoPorId(creditoId);
                if (credito == null) return;

                var ventana = new VentanaRegistrarPago(credito, usuarioActual);
                if (ventana.ShowDialog() == true)
                {
                    // Refrescar cliente para obtener nueva deuda
                    clienteActual = ClientesHelper.ObtenerClientePorId(clienteActual.Id);
                    CargarDatos();
                }
            }
        }

        private void BtnActualizarCreditos_Click(object sender, RoutedEventArgs e)
        {
            clienteActual = ClientesHelper.ObtenerClientePorId(clienteActual.Id);
            CargarDatos();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
