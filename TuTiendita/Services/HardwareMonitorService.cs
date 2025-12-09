using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace TuTiendita.Services
{
    /// <summary>
    /// Servicio para monitorear el estado de hardware (básculas, impresoras, etc.)
    /// </summary>
    public class HardwareMonitorService : IDisposable
    {
        private Timer _monitorTimer;
        private readonly int _intervaloMs;
        private bool _disposed = false;
        private bool _isMonitoring = false;

        // Estado actual del hardware
        public HardwareStatus EstadoActual { get; private set; }

        // Configuración de dispositivos
        public ConfiguracionHardware Configuracion { get; set; }

        // Eventos para notificar cambios
        public event EventHandler<HardwareStatus> EstadoCambiado;
        public event EventHandler<DispositivoEventArgs> DispositivoConectado;
        public event EventHandler<DispositivoEventArgs> DispositivoDesconectado;

        public HardwareMonitorService(int intervaloSegundos = 5)
        {
            _intervaloMs = intervaloSegundos * 1000;
            EstadoActual = new HardwareStatus();
            Configuracion = new ConfiguracionHardware();
        }

        #region Control del Monitor

        /// <summary>
        /// Inicia el monitoreo continuo de hardware
        /// </summary>
        public void IniciarMonitoreo()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _monitorTimer = new Timer(
                async _ => await VerificarHardwareAsync(),
                null,
                0, // Ejecutar inmediatamente
                _intervaloMs);
        }

        /// <summary>
        /// Detiene el monitoreo
        /// </summary>
        public void DetenerMonitoreo()
        {
            _isMonitoring = false;
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        /// <summary>
        /// Verifica el estado de todo el hardware
        /// </summary>
        public async Task<HardwareStatus> VerificarHardwareAsync()
        {
            var nuevoEstado = new HardwareStatus
            {
                UltimaVerificacion = DateTime.Now
            };

            try
            {
                // Verificar en paralelo para mayor velocidad
                var tareas = new List<Task>
                {
                    Task.Run(() => VerificarBascula(nuevoEstado)),
                    Task.Run(() => VerificarImpresoras(nuevoEstado)),
                    Task.Run(() => VerificarPuertosSeriales(nuevoEstado)),
                    Task.Run(() => VerificarConexionRed(nuevoEstado))
                };

                await Task.WhenAll(tareas);

                // Detectar cambios y notificar
                DetectarCambios(EstadoActual, nuevoEstado);
                EstadoActual = nuevoEstado;

                // Notificar cambio de estado
                EstadoCambiado?.Invoke(this, nuevoEstado);
            }
            catch (Exception ex)
            {
                nuevoEstado.ErrorGeneral = ex.Message;
            }

            return nuevoEstado;
        }

        #endregion

        #region Verificación de Báscula

        private void VerificarBascula(HardwareStatus estado)
        {
            try
            {
                if (string.IsNullOrEmpty(Configuracion.PuertoBascula))
                {
                    estado.Bascula = new EstadoDispositivo
                    {
                        Nombre = "Báscula",
                        Estado = EstadoConexion.NoConfigurado,
                        Mensaje = "Puerto no configurado"
                    };
                    return;
                }

                // Verificar si el puerto existe
                var puertosDisponibles = SerialPort.GetPortNames();
                bool puertoExiste = puertosDisponibles.Contains(Configuracion.PuertoBascula);

                if (!puertoExiste)
                {
                    estado.Bascula = new EstadoDispositivo
                    {
                        Nombre = "Báscula",
                        Estado = EstadoConexion.Desconectado,
                        Puerto = Configuracion.PuertoBascula,
                        Mensaje = $"Puerto {Configuracion.PuertoBascula} no disponible"
                    };
                    return;
                }

                // Intentar abrir el puerto
                bool puertoFuncional = VerificarPuertoSerial(
                    Configuracion.PuertoBascula,
                    Configuracion.BaudRateBascula);

                estado.Bascula = new EstadoDispositivo
                {
                    Nombre = "Báscula",
                    Estado = puertoFuncional ? EstadoConexion.Conectado : EstadoConexion.Error,
                    Puerto = Configuracion.PuertoBascula,
                    Mensaje = puertoFuncional ? "Conectada y funcionando" : "Error de comunicación"
                };
            }
            catch (Exception ex)
            {
                estado.Bascula = new EstadoDispositivo
                {
                    Nombre = "Báscula",
                    Estado = EstadoConexion.Error,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        private bool VerificarPuertoSerial(string puerto, int baudRate)
        {
            SerialPort serialPort = null;
            try
            {
                serialPort = new SerialPort(puerto, baudRate)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                serialPort.Open();
                bool estaAbierto = serialPort.IsOpen;
                serialPort.Close();

                return estaAbierto;
            }
            catch (UnauthorizedAccessException)
            {
                // Puerto en uso por otra aplicación - puede estar funcionando
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    serialPort?.Close();
                    serialPort?.Dispose();
                }
                catch { }
            }
        }

        #endregion

        #region Verificación de Impresoras

        private void VerificarImpresoras(HardwareStatus estado)
        {
            try
            {
                estado.Impresoras = new List<EstadoDispositivo>();

                // Obtener todas las impresoras instaladas
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var estadoImpresora = VerificarImpresoraIndividual(printerName);
                    estado.Impresoras.Add(estadoImpresora);
                }

                // Verificar impresora de tickets específica si está configurada
                if (!string.IsNullOrEmpty(Configuracion.ImpresoraTickets))
                {
                    var impTickets = estado.Impresoras
                        .FirstOrDefault(i => i.Nombre == Configuracion.ImpresoraTickets);

                    estado.ImpresoraTickets = impTickets ?? new EstadoDispositivo
                    {
                        Nombre = Configuracion.ImpresoraTickets,
                        Estado = EstadoConexion.Desconectado,
                        Mensaje = "Impresora no encontrada"
                    };
                }

                // Verificar impresora de reportes específica si está configurada
                if (!string.IsNullOrEmpty(Configuracion.ImpresoraReportes))
                {
                    var impReportes = estado.Impresoras
                        .FirstOrDefault(i => i.Nombre == Configuracion.ImpresoraReportes);

                    estado.ImpresoraReportes = impReportes ?? new EstadoDispositivo
                    {
                        Nombre = Configuracion.ImpresoraReportes,
                        Estado = EstadoConexion.Desconectado,
                        Mensaje = "Impresora no encontrada"
                    };
                }
            }
            catch (Exception ex)
            {
                estado.Impresoras = new List<EstadoDispositivo>
                {
                    new EstadoDispositivo
                    {
                        Nombre = "Error",
                        Estado = EstadoConexion.Error,
                        Mensaje = ex.Message
                    }
                };
            }
        }

        private EstadoDispositivo VerificarImpresoraIndividual(string nombreImpresora)
        {
            try
            {
                var settings = new PrinterSettings { PrinterName = nombreImpresora };
                bool esValida = settings.IsValid;

                // Intentar obtener más información con WMI
                string estadoWmi = ObtenerEstadoImpresoraWMI(nombreImpresora);
                bool esDefault = settings.IsDefaultPrinter;

                EstadoConexion estado;
                string mensaje;

                if (!esValida)
                {
                    estado = EstadoConexion.Error;
                    mensaje = "Impresora no válida";
                }
                else if (estadoWmi.Contains("Offline") || estadoWmi.Contains("Error"))
                {
                    estado = EstadoConexion.Desconectado;
                    mensaje = estadoWmi;
                }
                else if (estadoWmi.Contains("Paused"))
                {
                    estado = EstadoConexion.Pausado;
                    mensaje = "Impresora pausada";
                }
                else
                {
                    estado = EstadoConexion.Conectado;
                    mensaje = esDefault ? "Lista (Predeterminada)" : "Lista";
                }

                return new EstadoDispositivo
                {
                    Nombre = nombreImpresora,
                    Estado = estado,
                    Mensaje = mensaje,
                    EsDefault = esDefault
                };
            }
            catch (Exception ex)
            {
                return new EstadoDispositivo
                {
                    Nombre = nombreImpresora,
                    Estado = EstadoConexion.Error,
                    Mensaje = ex.Message
                };
            }
        }

        private string ObtenerEstadoImpresoraWMI(string nombreImpresora)
        {
            try
            {
                string query = $"SELECT * FROM Win32_Printer WHERE Name = '{nombreImpresora.Replace("\\", "\\\\")}'";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject printer in searcher.Get())
                    {
                        var estadoTrabajo = printer["PrinterStatus"]?.ToString();
                        var estadoCola = printer["PrinterState"]?.ToString();
                        bool enLinea = (bool)(printer["WorkOffline"] ?? false) == false;

                        if (!enLinea) return "Offline";

                        // Estados de impresora según WMI
                        switch (estadoTrabajo)
                        {
                            case "1": return "Other";
                            case "2": return "Unknown";
                            case "3": return "Idle - Lista";
                            case "4": return "Printing";
                            case "5": return "Warmup";
                            case "6": return "Stopped";
                            case "7": return "Offline";
                            default: return "Ready";
                        }
                    }
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        #region Verificación de Puertos Seriales

        private void VerificarPuertosSeriales(HardwareStatus estado)
        {
            try
            {
                estado.PuertosSeriales = new List<EstadoDispositivo>();
                var puertos = SerialPort.GetPortNames();

                foreach (var puerto in puertos)
                {
                    bool disponible = VerificarPuertoSerial(puerto, 9600);
                    estado.PuertosSeriales.Add(new EstadoDispositivo
                    {
                        Nombre = puerto,
                        Puerto = puerto,
                        Estado = disponible ? EstadoConexion.Conectado : EstadoConexion.EnUso,
                        Mensaje = disponible ? "Disponible" : "En uso"
                    });
                }
            }
            catch (Exception ex)
            {
                estado.PuertosSeriales = new List<EstadoDispositivo>
                {
                    new EstadoDispositivo
                    {
                        Nombre = "Error",
                        Estado = EstadoConexion.Error,
                        Mensaje = ex.Message
                    }
                };
            }
        }

        #endregion

        #region Verificación de Red

        private void VerificarConexionRed(HardwareStatus estado)
        {
            try
            {
                estado.ConexionRed = new EstadoDispositivo
                {
                    Nombre = "Conexión a Internet"
                };

                // Verificar conectividad de red
                bool hayRed = NetworkInterface.GetIsNetworkAvailable();

                if (!hayRed)
                {
                    estado.ConexionRed.Estado = EstadoConexion.Desconectado;
                    estado.ConexionRed.Mensaje = "Sin conexión de red";
                    return;
                }

                // Intentar ping a Google DNS para verificar internet
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    if (reply.Status == IPStatus.Success)
                    {
                        estado.ConexionRed.Estado = EstadoConexion.Conectado;
                        estado.ConexionRed.Mensaje = $"Conectado ({reply.RoundtripTime}ms)";
                    }
                    else
                    {
                        estado.ConexionRed.Estado = EstadoConexion.Error;
                        estado.ConexionRed.Mensaje = "Sin acceso a Internet";
                    }
                }
            }
            catch (Exception ex)
            {
                estado.ConexionRed = new EstadoDispositivo
                {
                    Nombre = "Conexión a Internet",
                    Estado = EstadoConexion.Error,
                    Mensaje = ex.Message
                };
            }
        }

        #endregion

        #region Detección de Cambios

        private void DetectarCambios(HardwareStatus anterior, HardwareStatus nuevo)
        {
            if (anterior == null) return;

            // Verificar cambio en báscula
            if (anterior.Bascula?.Estado != nuevo.Bascula?.Estado)
            {
                var args = new DispositivoEventArgs
                {
                    Dispositivo = nuevo.Bascula,
                    TipoDispositivo = TipoDispositivo.Bascula
                };

                if (nuevo.Bascula?.Estado == EstadoConexion.Conectado)
                    DispositivoConectado?.Invoke(this, args);
                else if (nuevo.Bascula?.Estado == EstadoConexion.Desconectado)
                    DispositivoDesconectado?.Invoke(this, args);
            }

            // Verificar cambio en impresora de tickets
            if (anterior.ImpresoraTickets?.Estado != nuevo.ImpresoraTickets?.Estado)
            {
                var args = new DispositivoEventArgs
                {
                    Dispositivo = nuevo.ImpresoraTickets,
                    TipoDispositivo = TipoDispositivo.ImpresoraTickets
                };

                if (nuevo.ImpresoraTickets?.Estado == EstadoConexion.Conectado)
                    DispositivoConectado?.Invoke(this, args);
                else if (nuevo.ImpresoraTickets?.Estado == EstadoConexion.Desconectado)
                    DispositivoDesconectado?.Invoke(this, args);
            }

            // Verificar cambio en conexión de red
            if (anterior.ConexionRed?.Estado != nuevo.ConexionRed?.Estado)
            {
                var args = new DispositivoEventArgs
                {
                    Dispositivo = nuevo.ConexionRed,
                    TipoDispositivo = TipoDispositivo.Red
                };

                if (nuevo.ConexionRed?.Estado == EstadoConexion.Conectado)
                    DispositivoConectado?.Invoke(this, args);
                else if (nuevo.ConexionRed?.Estado == EstadoConexion.Desconectado)
                    DispositivoDesconectado?.Invoke(this, args);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DetenerMonitoreo();
                }
                _disposed = true;
            }
        }

        #endregion
    }

    #region Modelos

    public class HardwareStatus
    {
        public DateTime UltimaVerificacion { get; set; }
        public string ErrorGeneral { get; set; }

        public EstadoDispositivo Bascula { get; set; }
        public EstadoDispositivo ImpresoraTickets { get; set; }
        public EstadoDispositivo ImpresoraReportes { get; set; }
        public EstadoDispositivo ConexionRed { get; set; }

        public List<EstadoDispositivo> Impresoras { get; set; } = new List<EstadoDispositivo>();
        public List<EstadoDispositivo> PuertosSeriales { get; set; } = new List<EstadoDispositivo>();

        public bool TodoConectado =>
            (Bascula?.Estado == EstadoConexion.Conectado || Bascula?.Estado == EstadoConexion.NoConfigurado) &&
            (ImpresoraTickets?.Estado == EstadoConexion.Conectado || ImpresoraTickets == null) &&
            (ConexionRed?.Estado == EstadoConexion.Conectado);

        public int DispositivosConProblemas =>
            (Bascula?.Estado == EstadoConexion.Desconectado || Bascula?.Estado == EstadoConexion.Error ? 1 : 0) +
            (ImpresoraTickets?.Estado == EstadoConexion.Desconectado || ImpresoraTickets?.Estado == EstadoConexion.Error ? 1 : 0) +
            (ConexionRed?.Estado == EstadoConexion.Desconectado || ConexionRed?.Estado == EstadoConexion.Error ? 1 : 0);
    }

    public class EstadoDispositivo
    {
        public string Nombre { get; set; }
        public EstadoConexion Estado { get; set; }
        public string Puerto { get; set; }
        public string Mensaje { get; set; }
        public bool EsDefault { get; set; }

        public string IconoEstado => Estado switch
        {
            EstadoConexion.Conectado => "✓",
            EstadoConexion.Desconectado => "✗",
            EstadoConexion.Error => "⚠",
            EstadoConexion.Pausado => "⏸",
            EstadoConexion.EnUso => "🔒",
            EstadoConexion.NoConfigurado => "○",
            _ => "?"
        };

        public string ColorEstado => Estado switch
        {
            EstadoConexion.Conectado => "#27AE60",
            EstadoConexion.Desconectado => "#E74C3C",
            EstadoConexion.Error => "#E67E22",
            EstadoConexion.Pausado => "#F39C12",
            EstadoConexion.EnUso => "#3498DB",
            EstadoConexion.NoConfigurado => "#95A5A6",
            _ => "#7F8C8D"
        };
    }

    // ConfiguracionHardware se encuentra en ConfiguracionHardware.cs

    public enum EstadoConexion
    {
        Conectado,
        Desconectado,
        Error,
        Pausado,
        EnUso,
        NoConfigurado,
        Verificando
    }

    public enum TipoDispositivo
    {
        Bascula,
        ImpresoraTickets,
        ImpresoraReportes,
        CajonDinero,
        LectorCodigoBarras,
        Red
    }

    public class DispositivoEventArgs : EventArgs
    {
        public EstadoDispositivo Dispositivo { get; set; }
        public TipoDispositivo TipoDispositivo { get; set; }
    }

    #endregion
}
