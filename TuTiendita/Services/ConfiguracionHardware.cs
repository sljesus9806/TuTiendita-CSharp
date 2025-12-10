using System;
using System.IO;
using System.Text.Json;

namespace TuTiendita.Services
{
    /// <summary>
    /// Configuración de hardware del sistema (báscula, impresoras, etc.)
    /// </summary>
    public class ConfiguracionHardware
    {
        // Báscula
        public string PuertoBascula { get; set; } = "";
        public int BaudRateBascula { get; set; } = 9600;
        public int BitsDatosBascula { get; set; } = 8;
        public string ParidadBascula { get; set; } = "None";
        public string BitStopBascula { get; set; } = "One";

        // Impresoras
        public string ImpresoraTickets { get; set; } = "";
        public string ImpresoraReportes { get; set; } = "";
        public string AnchoTicket { get; set; } = "80mm";

        // Cajón de dinero
        public bool CajonConectado { get; set; } = true;
        public string ComandoAbrirCajon { get; set; } = "\x1B\x70\x00\x19\xFA"; // ESC/POS estándar

        // Lector de código de barras
        public string PuertoLectorCodigoBarras { get; set; } = "";

        // Monitoreo
        public bool MonitoreoActivo { get; set; } = true;
        public int IntervaloMonitoreoSegundos { get; set; } = 30;

        // Archivo de configuración
        private static readonly string ArchivoConfig = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TuTiendita",
            "hardware.json"
        );

        /// <summary>
        /// Carga la configuración desde el archivo JSON
        /// </summary>
        public static ConfiguracionHardware Cargar()
        {
            try
            {
                if (File.Exists(ArchivoConfig))
                {
                    var json = File.ReadAllText(ArchivoConfig);
                    return JsonSerializer.Deserialize<ConfiguracionHardware>(json)
                           ?? new ConfiguracionHardware();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar configuracion de hardware: {ex.Message}");
            }

            return new ConfiguracionHardware();
        }

        /// <summary>
        /// Guarda la configuración en el archivo JSON
        /// </summary>
        public void Guardar()
        {
            try
            {
                var directorio = Path.GetDirectoryName(ArchivoConfig);
                if (!string.IsNullOrEmpty(directorio) && !Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                var opciones = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, opciones);
                File.WriteAllText(ArchivoConfig, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar configuracion de hardware: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Instancia singleton para acceso global
        /// </summary>
        private static ConfiguracionHardware _instancia;
        private static readonly object _lock = new object();

        public static ConfiguracionHardware Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    lock (_lock)
                    {
                        if (_instancia == null)
                        {
                            _instancia = Cargar();
                        }
                    }
                }
                return _instancia;
            }
        }

        /// <summary>
        /// Recarga la configuración desde el archivo
        /// </summary>
        public static void Recargar()
        {
            lock (_lock)
            {
                _instancia = Cargar();
            }
        }
    }
}
