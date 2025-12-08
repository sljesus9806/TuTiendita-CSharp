using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TuTiendita
{
    /// <summary>
    /// Ventana para seleccionar un cliente con datos fiscales
    /// </summary>
    public partial class VentanaSeleccionarClienteFiscal : Window
    {
        private List<ClienteFiscal> _clientes;

        public ClienteFiscal ClienteSeleccionado { get; private set; }

        public VentanaSeleccionarClienteFiscal()
        {
            InitializeComponent();
            CargarClientes();
        }

        private void CargarClientes()
        {
            try
            {
                _clientes = new List<ClienteFiscal>();

                using (var conn = Database.GetConnection())
                {
                    conn.Open();
                    // Solo cargar clientes que tengan RFC registrado
                    string query = @"SELECT * FROM Clientes
                                   WHERE RFC IS NOT NULL AND RFC != '' AND Activo = 1
                                   ORDER BY Nombre";

                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _clientes.Add(new ClienteFiscal
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nombre = reader["Nombre"]?.ToString(),
                                Apellido = reader["Apellido"]?.ToString(),
                                Documento = reader["Documento"]?.ToString(),
                                RFC = reader["RFC"]?.ToString(),
                                RazonSocial = reader["RazonSocial"]?.ToString(),
                                RegimenFiscalClave = reader["RegimenFiscalClave"]?.ToString(),
                                DomicilioFiscalCP = reader["DomicilioFiscalCP"]?.ToString(),
                                UsoCFDIDefault = reader["UsoCFDIDefault"]?.ToString()
                            });
                        }
                    }
                }

                dgClientes.ItemsSource = _clientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = txtBuscar.Text?.Trim().ToLower();

            if (string.IsNullOrEmpty(busqueda))
            {
                dgClientes.ItemsSource = _clientes;
            }
            else
            {
                var filtrados = _clientes.Where(c =>
                    (c.NombreCompleto?.ToLower().Contains(busqueda) ?? false) ||
                    (c.RFC?.ToLower().Contains(busqueda) ?? false) ||
                    (c.RazonSocial?.ToLower().Contains(busqueda) ?? false) ||
                    (c.Documento?.ToLower().Contains(busqueda) ?? false)
                ).ToList();

                dgClientes.ItemsSource = filtrados;
            }
        }

        private void dgClientes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Seleccionar();
        }

        private void btnSeleccionar_Click(object sender, RoutedEventArgs e)
        {
            Seleccionar();
        }

        private void Seleccionar()
        {
            if (dgClientes.SelectedItem is ClienteFiscal cliente)
            {
                ClienteSeleccionado = cliente;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Seleccione un cliente de la lista.", "Información",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// Modelo de cliente con datos fiscales
    /// </summary>
    public class ClienteFiscal
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Documento { get; set; }
        public string RFC { get; set; }
        public string RazonSocial { get; set; }
        public string RegimenFiscalClave { get; set; }
        public string DomicilioFiscalCP { get; set; }
        public string UsoCFDIDefault { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
    }
}
