using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TuTiendita.Services;

namespace TuTiendita
{
    public partial class VentanaConfigHardware : Window
    {
        private readonly HardwareMonitorService _hardwareMonitor;
        private string _puertoBasculaActual;
        private int _baudRateActual;

        public VentanaConfigHardware()
        {
            InitializeComponent();
            _hardwareMonitor = new HardwareMonitorService();
            Loaded += VentanaConfigHardware_Loaded;
        }

        private async void VentanaConfigHardware_Loaded(object sender, RoutedEventArgs e)
        {
            CargarPuertosCOM();
            CargarImpresoras();
            CargarConfiguracionActual();
            await ActualizarEstadoDispositivos();
        }

        private void CargarPuertosCOM()
        {
            try
            {
                cmbPuertoBascula.Items.Clear();
                cmbPuertoBascula.Items.Add(new ComboBoxItem { Content = "(Sin bascula)" });

                var puertos = SerialPort.GetPortNames();
                foreach (var puerto in puertos.OrderBy(p => p))
                {
                    cmbPuertoBascula.Items.Add(new ComboBoxItem { Content = puerto });
                }

                cmbPuertoBascula.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar puertos COM: {ex.Message}");
            }
        }

        private void CargarImpresoras()
        {
            try
            {
                cmbImpresoraTickets.Items.Clear();
                cmbImpresoraTickets.Items.Add(new ComboBoxItem { Content = "(Usar impresora predeterminada)" });

                cmbImpresoraReportes.Items.Clear();
                cmbImpresoraReportes.Items.Add(new ComboBoxItem { Content = "(Misma que tickets)" });

                using (var servidor = new LocalPrintServer())
                {
                    var impresoras = servidor.GetPrintQueues();
                    foreach (var impresora in impresoras.OrderBy(i => i.Name))
                    {
                        cmbImpresoraTickets.Items.Add(new ComboBoxItem { Content = impresora.Name });
                        cmbImpresoraReportes.Items.Add(new ComboBoxItem { Content = impresora.Name });
                    }
                }

                cmbImpresoraTickets.SelectedIndex = 0;
                cmbImpresoraReportes.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar impresoras: {ex.Message}");
            }
        }

        private void CargarConfiguracionActual()
        {
            try
            {
                var config = ConfiguracionHardware.Instancia;

                // Cargar puerto de báscula
                _puertoBasculaActual = config.PuertoBascula ?? "";
                if (!string.IsNullOrEmpty(_puertoBasculaActual))
                {
                    for (int i = 0; i < cmbPuertoBascula.Items.Count; i++)
                    {
                        if ((cmbPuertoBascula.Items[i] as ComboBoxItem)?.Content?.ToString() == _puertoBasculaActual)
                        {
                            cmbPuertoBascula.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Cargar baud rate
                _baudRateActual = config.BaudRateBascula;
                if (_baudRateActual > 0)
                {
                    for (int i = 0; i < cmbBaudRate.Items.Count; i++)
                    {
                        if ((cmbBaudRate.Items[i] as ComboBoxItem)?.Content?.ToString() == _baudRateActual.ToString())
                        {
                            cmbBaudRate.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Cargar impresora de tickets
                var impresoraTickets = config.ImpresoraTickets ?? "";
                if (!string.IsNullOrEmpty(impresoraTickets))
                {
                    for (int i = 0; i < cmbImpresoraTickets.Items.Count; i++)
                    {
                        if ((cmbImpresoraTickets.Items[i] as ComboBoxItem)?.Content?.ToString() == impresoraTickets)
                        {
                            cmbImpresoraTickets.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Cargar impresora de reportes
                var impresoraReportes = config.ImpresoraReportes ?? "";
                if (!string.IsNullOrEmpty(impresoraReportes))
                {
                    for (int i = 0; i < cmbImpresoraReportes.Items.Count; i++)
                    {
                        if ((cmbImpresoraReportes.Items[i] as ComboBoxItem)?.Content?.ToString() == impresoraReportes)
                        {
                            cmbImpresoraReportes.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Cargar ancho de ticket
                var anchoTicket = config.AnchoTicket ?? "80mm";
                for (int i = 0; i < cmbAnchoTicket.Items.Count; i++)
                {
                    if ((cmbAnchoTicket.Items[i] as ComboBoxItem)?.Content?.ToString() == anchoTicket)
                    {
                        cmbAnchoTicket.SelectedIndex = i;
                        break;
                    }
                }

                // Cargar estado del cajón
                chkCajonConectado.IsChecked = config.CajonConectado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar configuracion: {ex.Message}");
            }
        }

        private async Task ActualizarEstadoDispositivos()
        {
            try
            {
                txtEstadoActual.Text = "Verificando dispositivos...";

                var estado = await _hardwareMonitor.VerificarHardwareAsync();

                var sb = new StringBuilder();

                // Estado de báscula
                bool basculaConectada = estado.Bascula?.Estado == EstadoConexion.Conectado;
                sb.AppendLine($"• Bascula: {(basculaConectada ? "✓ Conectada" : "✗ No detectada")}");
                if (basculaConectada && !string.IsNullOrEmpty(estado.Bascula?.Puerto))
                {
                    sb.AppendLine($"  Puerto: {estado.Bascula.Puerto}");
                }
                if (estado.Bascula != null && !string.IsNullOrEmpty(estado.Bascula.Mensaje))
                {
                    sb.AppendLine($"  Estado: {estado.Bascula.Mensaje}");
                }

                // Estado de impresora
                bool impresoraDisponible = estado.ImpresoraTickets?.Estado == EstadoConexion.Conectado ||
                                           estado.Impresoras?.Any(i => i.Estado == EstadoConexion.Conectado) == true;
                sb.AppendLine($"• Impresora: {(impresoraDisponible ? "✓ Lista" : "✗ No disponible")}");

                if (estado.ImpresoraTickets != null)
                {
                    sb.AppendLine($"  Nombre: {estado.ImpresoraTickets.Nombre}");
                    sb.AppendLine($"  Estado: {estado.ImpresoraTickets.Mensaje}");
                }
                else if (estado.Impresoras?.Count > 0)
                {
                    var primeraImpresora = estado.Impresoras.FirstOrDefault(i => i.EsDefault) ?? estado.Impresoras[0];
                    sb.AppendLine($"  Nombre: {primeraImpresora.Nombre}");
                    sb.AppendLine($"  Estado: {primeraImpresora.Mensaje}");
                }

                // Estado de internet
                bool internetDisponible = estado.ConexionRed?.Estado == EstadoConexion.Conectado;
                sb.AppendLine($"• Internet: {(internetDisponible ? "✓ Conectado" : "✗ Sin conexion")}");
                if (estado.ConexionRed != null && !string.IsNullOrEmpty(estado.ConexionRed.Mensaje))
                {
                    sb.AppendLine($"  Detalle: {estado.ConexionRed.Mensaje}");
                }

                // Última verificación
                sb.AppendLine();
                sb.AppendLine($"Ultima verificacion: {estado.UltimaVerificacion:HH:mm:ss}");

                txtEstadoActual.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                txtEstadoActual.Text = $"Error al verificar dispositivos: {ex.Message}";
            }
        }

        private async void btnProbarBascula_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var contenidoOriginal = btn?.Content;

            try
            {
                if (btn != null)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Probando...";
                }

                var puertoSeleccionado = (cmbPuertoBascula.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var baudRateStr = (cmbBaudRate.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (string.IsNullOrEmpty(puertoSeleccionado) || puertoSeleccionado == "(Sin bascula)")
                {
                    MessageBox.Show("Por favor seleccione un puerto COM para la bascula.",
                        "Puerto no seleccionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(baudRateStr, out int baudRate))
                {
                    baudRate = 9600;
                }

                var resultado = await Task.Run(() =>
                {
                    try
                    {
                        using (var puerto = new SerialPort(puertoSeleccionado, baudRate))
                        {
                            puerto.ReadTimeout = 3000;
                            puerto.WriteTimeout = 3000;
                            puerto.Open();

                            // Intentar leer datos de la báscula
                            System.Threading.Thread.Sleep(500);

                            if (puerto.BytesToRead > 0)
                            {
                                var datos = puerto.ReadExisting();
                                return $"Conexion exitosa!\n\nDatos recibidos:\n{datos}";
                            }
                            else
                            {
                                return "Conexion establecida.\n\nNo se recibieron datos automaticamente.\nAlgunas basculas requieren enviar un comando para obtener el peso.";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return $"Error de conexion:\n{ex.Message}";
                    }
                });

                MessageBox.Show(resultado, "Prueba de Bascula",
                    MessageBoxButton.OK,
                    resultado.StartsWith("Error") ? MessageBoxImage.Error : MessageBoxImage.Information);
            }
            finally
            {
                if (btn != null)
                {
                    btn.IsEnabled = true;
                    btn.Content = contenidoOriginal;
                }
            }
        }

        private void btnProbarImpresora_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var contenidoOriginal = btn?.Content;

            try
            {
                if (btn != null)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Imprimiendo...";
                }

                var impresoraSeleccionada = (cmbImpresoraTickets.SelectedItem as ComboBoxItem)?.Content?.ToString();
                var anchoTicket = (cmbAnchoTicket.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "80mm";

                // Crear documento de prueba
                var printDialog = new System.Windows.Controls.PrintDialog();

                // Si hay impresora específica seleccionada, usarla
                if (!string.IsNullOrEmpty(impresoraSeleccionada) && impresoraSeleccionada != "(Usar impresora predeterminada)")
                {
                    try
                    {
                        using (var servidor = new LocalPrintServer())
                        {
                            var cola = servidor.GetPrintQueues()
                                .FirstOrDefault(q => q.Name == impresoraSeleccionada);
                            if (cola != null)
                            {
                                printDialog.PrintQueue = cola;
                            }
                        }
                    }
                    catch { }
                }

                // Crear contenido de prueba
                var documento = new System.Windows.Documents.FlowDocument();
                var parrafo1 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("=== PRUEBA DE IMPRESORA ==="))
                {
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold
                };
                documento.Blocks.Add(parrafo1);

                var parrafo2 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run($"\nFecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"))
                {
                    TextAlignment = TextAlignment.Center
                };
                documento.Blocks.Add(parrafo2);

                var parrafo3 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run($"Ancho papel: {anchoTicket}"))
                {
                    TextAlignment = TextAlignment.Center
                };
                documento.Blocks.Add(parrafo3);

                var parrafo4 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("\n--- Tu Tiendita ---"))
                {
                    TextAlignment = TextAlignment.Center
                };
                documento.Blocks.Add(parrafo4);

                var parrafo5 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("\nSi puede leer esto,\nla impresora funciona correctamente."))
                {
                    TextAlignment = TextAlignment.Center
                };
                documento.Blocks.Add(parrafo5);

                var parrafo6 = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("\n==========================="))
                {
                    TextAlignment = TextAlignment.Center
                };
                documento.Blocks.Add(parrafo6);

                // Configurar ancho según tamaño de papel
                double anchoDoc = anchoTicket == "58mm" ? 160 : 220;
                documento.PageWidth = anchoDoc;
                documento.PagePadding = new Thickness(5);

                var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)documento).DocumentPaginator;
                printDialog.PrintDocument(paginator, "Prueba TuTiendita");

                MessageBox.Show("Documento de prueba enviado a la impresora.",
                    "Prueba de Impresora", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir:\n{ex.Message}",
                    "Error de Impresion", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null)
                {
                    btn.IsEnabled = true;
                    btn.Content = contenidoOriginal;
                }
            }
        }

        private void btnAbrirCajon_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var contenidoOriginal = btn?.Content;

            try
            {
                if (btn != null)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Abriendo...";
                }

                if (chkCajonConectado.IsChecked != true)
                {
                    MessageBox.Show("El cajon no esta marcado como conectado.",
                        "Cajon no configurado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var impresoraSeleccionada = (cmbImpresoraTickets.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // Comandos ESC/POS comunes para abrir cajón
                // ESC p m t1 t2 - Donde m=0 o 1 (pin), t1 y t2 son tiempos
                byte[] comandoAbrirCajon = new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA };

                try
                {
                    // Intentar enviar comando directamente a la impresora
                    System.IO.File.WriteAllBytes("LPT1", comandoAbrirCajon);
                    MessageBox.Show("Comando de apertura enviado al cajon.",
                        "Cajon de Dinero", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    // Si falla LPT1, intentar con impresora seleccionada via RawPrinterHelper
                    MessageBox.Show(
                        "No se pudo enviar el comando directamente.\n\n" +
                        "Nota: Algunos cajones requieren configuracion especial.\n" +
                        "Verifique que:\n" +
                        "• El cajon este conectado al puerto RJ-11 de la impresora\n" +
                        "• La impresora este encendida\n" +
                        "• El cajon tenga alimentacion electrica (si es requerida)",
                        "Cajon de Dinero", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            finally
            {
                if (btn != null)
                {
                    btn.IsEnabled = true;
                    btn.Content = contenidoOriginal;
                }
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = ConfiguracionHardware.Instancia;

                // Guardar puerto de báscula
                var puertoBascula = (cmbPuertoBascula.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (puertoBascula == "(Sin bascula)")
                    puertoBascula = "";
                config.PuertoBascula = puertoBascula ?? "";

                // Guardar baud rate
                var baudRateStr = (cmbBaudRate.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (int.TryParse(baudRateStr, out int baudRate))
                {
                    config.BaudRateBascula = baudRate;
                }

                // Guardar impresora de tickets
                var impresoraTickets = (cmbImpresoraTickets.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (impresoraTickets == "(Usar impresora predeterminada)")
                    impresoraTickets = "";
                config.ImpresoraTickets = impresoraTickets ?? "";

                // Guardar impresora de reportes
                var impresoraReportes = (cmbImpresoraReportes.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (impresoraReportes == "(Misma que tickets)")
                    impresoraReportes = "";
                config.ImpresoraReportes = impresoraReportes ?? "";

                // Guardar ancho de ticket
                var anchoTicket = (cmbAnchoTicket.SelectedItem as ComboBoxItem)?.Content?.ToString();
                config.AnchoTicket = anchoTicket ?? "80mm";

                // Guardar estado del cajón
                config.CajonConectado = chkCajonConectado.IsChecked == true;

                // Persistir cambios
                config.Guardar();

                MessageBox.Show("Configuracion guardada correctamente.",
                    "Configuracion Guardada", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la configuracion:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _hardwareMonitor?.Dispose();
        }
    }
}
