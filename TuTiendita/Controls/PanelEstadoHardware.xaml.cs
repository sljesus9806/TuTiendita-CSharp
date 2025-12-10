using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TuTiendita.Services;

namespace TuTiendita.Controls
{
    /// <summary>
    /// Panel de estado de hardware que muestra conexión de báscula, impresoras y red
    /// </summary>
    public partial class PanelEstadoHardware : UserControl
    {
        private HardwareMonitorService _monitor;
        private bool _isRefreshing = false;

        public PanelEstadoHardware()
        {
            InitializeComponent();
            Loaded += PanelEstadoHardware_Loaded;
            Unloaded += PanelEstadoHardware_Unloaded;
        }

        #region Inicialización

        private void PanelEstadoHardware_Loaded(object sender, RoutedEventArgs e)
        {
            IniciarMonitoreo();
        }

        private void PanelEstadoHardware_Unloaded(object sender, RoutedEventArgs e)
        {
            DetenerMonitoreo();
        }

        public void IniciarMonitoreo()
        {
            if (_monitor != null) return;

            _monitor = new HardwareMonitorService(intervaloSegundos: 10);

            // Cargar configuración guardada
            CargarConfiguracion();

            // Suscribirse a eventos
            _monitor.EstadoCambiado += Monitor_EstadoCambiado;
            _monitor.DispositivoConectado += Monitor_DispositivoConectado;
            _monitor.DispositivoDesconectado += Monitor_DispositivoDesconectado;

            // Iniciar monitoreo
            _monitor.IniciarMonitoreo();
        }

        public void DetenerMonitoreo()
        {
            if (_monitor != null)
            {
                _monitor.EstadoCambiado -= Monitor_EstadoCambiado;
                _monitor.DispositivoConectado -= Monitor_DispositivoConectado;
                _monitor.DispositivoDesconectado -= Monitor_DispositivoDesconectado;
                _monitor.Dispose();
                _monitor = null;
            }
        }

        private void CargarConfiguracion()
        {
            try
            {
                // Cargar configuración desde el archivo JSON
                _monitor.Configuracion = ConfiguracionHardware.Instancia;
            }
            catch
            {
                // Error cargando configuración, usar valores por defecto
                _monitor.Configuracion = new ConfiguracionHardware();
            }
        }

        #endregion

        #region Eventos del Monitor

        private void Monitor_EstadoCambiado(object sender, HardwareStatus estado)
        {
            // Ejecutar en el hilo de UI de forma asíncrona para evitar deadlocks
            if (estado == null) return;

            if (Dispatcher.CheckAccess())
            {
                ActualizarUI(estado);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => ActualizarUI(estado)));
            }
        }

        private void Monitor_DispositivoConectado(object sender, DispositivoEventArgs e)
        {
            if (e?.Dispositivo == null) return;

            var nombre = e.Dispositivo.Nombre ?? "Dispositivo";
            if (Dispatcher.CheckAccess())
            {
                MostrarNotificacion($"{nombre} conectado", true);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => MostrarNotificacion($"{nombre} conectado", true)));
            }
        }

        private void Monitor_DispositivoDesconectado(object sender, DispositivoEventArgs e)
        {
            if (e?.Dispositivo == null) return;

            var nombre = e.Dispositivo.Nombre ?? "Dispositivo";
            if (Dispatcher.CheckAccess())
            {
                MostrarNotificacion($"{nombre} desconectado", false);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => MostrarNotificacion($"{nombre} desconectado", false)));
            }
        }

        #endregion

        #region Actualización de UI

        private void ActualizarUI(HardwareStatus estado)
        {
            try
            {
                // Actualizar báscula
                ActualizarIndicador(brdBascula, txtBasculaEstado, estado.Bascula, "Báscula");

                // Actualizar impresora
                ActualizarIndicador(brdImpresora, txtImpresoraEstado, estado.ImpresoraTickets, "Impresora");

                // Actualizar lector de código de barras
                ActualizarIndicador(brdLector, txtLectorEstado, estado.LectorCodigoBarras, "Lector");

                // Actualizar red
                ActualizarIndicador(brdRed, txtRedEstado, estado.ConexionRed, "Internet");

                // Actualizar hora
                txtUltimaVerificacion.Text = $"Última: {estado.UltimaVerificacion:HH:mm:ss}";

                // Mensaje general
                if (estado.DispositivosConProblemas > 0)
                {
                    txtMensajeEstado.Text = $"⚠ {estado.DispositivosConProblemas} dispositivo(s) con problemas";
                    txtMensajeEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                }
                else if (estado.TodoConectado)
                {
                    txtMensajeEstado.Text = "✓ Todos los dispositivos funcionando";
                    txtMensajeEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                }
                else
                {
                    txtMensajeEstado.Text = "";
                }
            }
            catch
            {
                // Error actualizando UI
            }
        }

        private void ActualizarIndicador(Border border, TextBlock estadoText, EstadoDispositivo dispositivo, string nombreDefault)
        {
            if (dispositivo == null)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));
                estadoText.Text = "○";
                border.ToolTip = $"{nombreDefault}: No configurado";
                return;
            }

            try
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dispositivo.ColorEstado));
                estadoText.Text = dispositivo.IconoEstado;
                border.ToolTip = $"{dispositivo.Nombre}: {dispositivo.Mensaje}";
            }
            catch
            {
                border.Background = new SolidColorBrush(Colors.Gray);
                estadoText.Text = "?";
            }
        }

        private void MostrarNotificacion(string mensaje, bool esConexion)
        {
            // Cambiar temporalmente el mensaje de estado
            string mensajeAnterior = txtMensajeEstado.Text;
            var colorAnterior = txtMensajeEstado.Foreground;

            txtMensajeEstado.Text = esConexion ? $"✓ {mensaje}" : $"✗ {mensaje}";
            txtMensajeEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(esConexion ? "#27AE60" : "#E74C3C"));

            // Restaurar después de 3 segundos
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                // Forzar una actualización del estado
                if (_monitor?.EstadoActual != null)
                {
                    ActualizarUI(_monitor.EstadoActual);
                }
            };
            timer.Start();
        }

        #endregion

        #region Eventos de UI

        private void Dispositivo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string tag)
            {
                string mensaje = "";
                EstadoDispositivo dispositivo = null;

                switch (tag)
                {
                    case "Bascula":
                        dispositivo = _monitor?.EstadoActual?.Bascula;
                        if (dispositivo != null)
                        {
                            mensaje = $"BÁSCULA\n\n" +
                                     $"Estado: {dispositivo.Estado}\n" +
                                     $"Puerto: {dispositivo.Puerto ?? "No configurado"}\n" +
                                     $"Mensaje: {dispositivo.Mensaje}";
                        }
                        else
                        {
                            mensaje = "BÁSCULA\n\nNo configurada.\n\nVaya a Configuración > Hardware para configurar.";
                        }
                        break;

                    case "Impresora":
                        dispositivo = _monitor?.EstadoActual?.ImpresoraTickets;
                        if (dispositivo != null)
                        {
                            mensaje = $"IMPRESORA DE TICKETS\n\n" +
                                     $"Nombre: {dispositivo.Nombre}\n" +
                                     $"Estado: {dispositivo.Estado}\n" +
                                     $"Mensaje: {dispositivo.Mensaje}";
                        }
                        else
                        {
                            var impresoras = _monitor?.EstadoActual?.Impresoras;
                            if (impresoras != null && impresoras.Count > 0)
                            {
                                mensaje = "IMPRESORAS DISPONIBLES\n\n";
                                foreach (var imp in impresoras)
                                {
                                    mensaje += $"• {imp.Nombre} - {imp.Mensaje}{(imp.EsDefault ? " (Predeterminada)" : "")}\n";
                                }
                                mensaje += "\nVaya a Configuración > Hardware para seleccionar.";
                            }
                            else
                            {
                                mensaje = "IMPRESORA\n\nNo hay impresoras disponibles.";
                            }
                        }
                        break;

                    case "Lector":
                        dispositivo = _monitor?.EstadoActual?.LectorCodigoBarras;
                        if (dispositivo != null)
                        {
                            mensaje = $"LECTOR DE CÓDIGO DE BARRAS\n\n" +
                                     $"Estado: {dispositivo.Estado}\n" +
                                     $"Puerto: {dispositivo.Puerto ?? "No configurado"}\n" +
                                     $"Mensaje: {dispositivo.Mensaje}";
                        }
                        else
                        {
                            mensaje = "LECTOR DE CÓDIGO DE BARRAS\n\nNo configurado.\n\nVaya a Configuración > Hardware para configurar.";
                        }
                        break;

                    case "Red":
                        dispositivo = _monitor?.EstadoActual?.ConexionRed;
                        if (dispositivo != null)
                        {
                            mensaje = $"CONEXIÓN A INTERNET\n\n" +
                                     $"Estado: {dispositivo.Estado}\n" +
                                     $"Detalle: {dispositivo.Mensaje}";
                        }
                        else
                        {
                            mensaje = "CONEXIÓN A INTERNET\n\nVerificando...";
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(mensaje))
                {
                    MessageBox.Show(mensaje, "Estado del Dispositivo",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void btnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            if (_isRefreshing || _monitor == null) return;

            _isRefreshing = true;
            btnRefrescar.IsEnabled = false;
            btnRefrescar.Content = "...";
            txtMensajeEstado.Text = "Verificando dispositivos...";

            try
            {
                var estado = await _monitor.VerificarHardwareAsync();
                ActualizarUI(estado);
            }
            catch (Exception ex)
            {
                txtMensajeEstado.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _isRefreshing = false;
                btnRefrescar.IsEnabled = true;
                btnRefrescar.Content = "↻";
            }
        }

        private void btnConfigHardware_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ventanaConfig = new VentanaConfigHardware();
                ventanaConfig.Owner = Window.GetWindow(this);

                if (ventanaConfig.ShowDialog() == true)
                {
                    // Recargar configuración y actualizar el monitor
                    ConfiguracionHardware.Recargar();
                    if (_monitor != null)
                    {
                        _monitor.Configuracion = ConfiguracionHardware.Instancia;

                        // Forzar verificación inmediata
                        btnRefrescar_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Métodos Públicos

        /// <summary>
        /// Obtiene el estado actual del hardware
        /// </summary>
        public HardwareStatus ObtenerEstadoActual()
        {
            return _monitor?.EstadoActual;
        }

        /// <summary>
        /// Actualiza la configuración del monitor
        /// </summary>
        public void ActualizarConfiguracion(ConfiguracionHardware config)
        {
            if (_monitor != null && config != null)
            {
                _monitor.Configuracion = config;
            }
        }

        #endregion
    }
}
