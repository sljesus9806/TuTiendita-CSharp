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
        private bool _isVerifying = false; // Prevenir reentrancia
        private readonly object _lockObject = new object();

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
                async _ =>
                {
                    try
                    {
                        await VerificarHardwareAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error en monitoreo de hardware: {ex.Message}");
                    }
                },
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
            // Prevenir reentrancia - si ya hay una verificación en curso, retornar estado actual
            lock (_lockObject)
            {
                if (_isVerifying)
                {
                    return EstadoActual ?? new HardwareStatus();
                }
                _isVerifying = true;
            }

            var nuevoEstado = new HardwareStatus
            {
                UltimaVerificacion = DateTime.Now
            };

            try
            {
                // Crear objetos de resultado separados para evitar race conditions
                EstadoDispositivo resultadoBascula = null;
                EstadoDispositivo resultadoRed = null;
                List<EstadoDispositivo> resultadoImpresoras = null;
                List<EstadoDispositivo> resultadoPuertos = null;
                EstadoDispositivo resultadoImpTickets = null;
                EstadoDispositivo resultadoImpReportes = null;

                // Verificar en paralelo usando resultados separados
                var tareas = new List<Task>
                {
                    Task.Run(() => { resultadoBascula = VerificarBasculaSeguro(); }),
                    Task.Run(() =>
                    {
                        var resultado = VerificarImpresorasSeguro();
                        resultadoImpresoras = resultado.Impresoras;
                        resultadoImpTickets = resultado.ImpresoraTickets;
                        resultadoImpReportes = resultado.ImpresoraReportes;
                    }),
                    Task.Run(() => { resultadoPuertos = VerificarPuertosSeriales(); }),
                    Task.Run(() => { resultadoRed = VerificarConexionRedSeguro(); })
                };

                await Task.WhenAll(tareas);

                // Asignar resultados de forma segura
                nuevoEstado.Bascula = resultadoBascula;
                nuevoEstado.ConexionRed = resultadoRed;
                nuevoEstado.Impresoras = resultadoImpresoras ?? new List<EstadoDispositivo>();
                nuevoEstado.PuertosSeriales = resultadoPuertos ?? new List<EstadoDispositivo>();
                nuevoEstado.ImpresoraTickets = resultadoImpTickets;
                nuevoEstado.ImpresoraReportes = resultadoImpReportes;

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
            finally
            {
                lock (_lockObject)
                {
                    _isVerifying = false;
                }
            }

            return nuevoEstado;
        }

        #endregion

        #region Verificación de Báscula

        private EstadoDispositivo VerificarBasculaSeguro()
        {
            try
            {
                if (string.IsNullOrEmpty(Configuracion?.PuertoBascula))
                {
                    return new EstadoDispositivo
                    {
                        Nombre = "Báscula",
                        Estado = EstadoConexion.NoConfigurado,
                        Mensaje = "Puerto no configurado"
                    };
                }

                // Verificar si el puerto existe
                var puertosDisponibles = SerialPort.GetPortNames();
                bool puertoExiste = puertosDisponibles.Contains(Configuracion.PuertoBascula);

                if (!puertoExiste)
                {
                    return new EstadoDispositivo
                    {
                        Nombre = "Báscula",
                        Estado = EstadoConexion.Desconectado,
                        Puerto = Configuracion.PuertoBascula,
                        Mensaje = $"Puerto {Configuracion.PuertoBascula} no disponible"
                    };
                }

                // Intentar abrir el puerto
                bool puertoFuncional = VerificarPuertoSerial(
                    Configuracion.PuertoBascula,
                    Configuracion.BaudRateBascula);

                return new EstadoDispositivo
                {
                    Nombre = "Báscula",
                    Estado = puertoFuncional ? EstadoConexion.Conectado : EstadoConexion.Error,
                    Puerto = Configuracion.PuertoBascula,
                    Mensaje = puertoFuncional ? "Conectada y funcionando" : "Error de comunicación"
                };
            }
            catch (Exception ex)
            {
                return new EstadoDispositivo
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

        private (List<EstadoDispositivo> Impresoras, EstadoDispositivo ImpresoraTickets, EstadoDispositivo ImpresoraReportes) VerificarImpresorasSeguro()
        {
            var impresoras = new List<EstadoDispositivo>();
            EstadoDispositivo impTickets = null;
            EstadoDispositivo impReportes = null;

            try
            {
                // Obtener todas las impresoras instaladas
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    var estadoImpresora = VerificarImpresoraIndividual(printerName);
                    impresoras.Add(estadoImpresora);
                }

                // Verificar impresora de tickets específica si está configurada
                if (!string.IsNullOrEmpty(Configuracion?.ImpresoraTickets))
                {
                    impTickets = impresoras
                        .FirstOrDefault(i => i.Nombre == Configuracion.ImpresoraTickets);

                    if (impTickets == null)
                    {
                        impTickets = new EstadoDispositivo
                        {
                            Nombre = Configuracion.ImpresoraTickets,
                            Estado = EstadoConexion.Desconectado,
                            Mensaje = "Impresora no encontrada"
                        };
                    }
                }

                // Verificar impresora de reportes específica si está configurada
                if (!string.IsNullOrEmpty(Configuracion?.ImpresoraReportes))
                {
                    impReportes = impresoras
                        .FirstOrDefault(i => i.Nombre == Configuracion.ImpresoraReportes);

                    if (impReportes == null)
                    {
                        impReportes = new EstadoDispositivo
                        {
                            Nombre = Configuracion.ImpresoraReportes,
                            Estado = EstadoConexion.Desconectado,
                            Mensaje = "Impresora no encontrada"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                impresoras.Add(new EstadoDispositivo
                {
                    Nombre = "Error",
                    Estado = EstadoConexion.Error,
                    Mensaje = ex.Message
                });
            }

            return (impresoras, impTickets, impReportes);
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

        private List<EstadoDispositivo> VerificarPuertosSeriales()
        {
            var resultado = new List<EstadoDispositivo>();

            try
            {
                var puertos = SerialPort.GetPortNames();

                foreach (var puerto in puertos)
                {
                    bool disponible = VerificarPuertoSerial(puerto, 9600);
                    resultado.Add(new EstadoDispositivo
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
                resultado.Add(new EstadoDispositivo
                {
                    Nombre = "Error",
                    Estado = EstadoConexion.Error,
                    Mensaje = ex.Message
                });
            }

            return resultado;
        }

        #endregion

        #region Verificación de Red

        private EstadoDispositivo VerificarConexionRedSeguro()
        {
            try
            {
                // Verificar conectividad de red
                bool hayRed = NetworkInterface.GetIsNetworkAvailable();

                if (!hayRed)
                {
                    return new EstadoDispositivo
                    {
                        Nombre = "Conexión a Internet",
                        Estado = EstadoConexion.Desconectado,
                        Mensaje = "Sin conexión de red"
                    };
                }

                // Intentar ping a Google DNS para verificar internet
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 3000);
                    if (reply.Status == IPStatus.Success)
                    {
                        return new EstadoDispositivo
                        {
                            Nombre = "Conexión a Internet",
                            Estado = EstadoConexion.Conectado,
                            Mensaje = $"Conectado ({reply.RoundtripTime}ms)"
                        };
                    }
                    else
                    {
                        return new EstadoDispositivo
                        {
                            Nombre = "Conexión a Internet",
                            Estado = EstadoConexion.Error,
                            Mensaje = "Sin acceso a Internet"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new EstadoDispositivo
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
