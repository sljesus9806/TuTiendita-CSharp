using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Gestor de backups automáticos y manuales de la base de datos
    /// </summary>
    public static class BackupManager
    {
        private static readonly string BackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TuTiendita",
            "Backups"
        );

        /// <summary>
        /// Inicializa el sistema de backups (crea directorio si no existe)
        /// </summary>
        public static void Inicializar()
        {
            try
            {
                if (!Directory.Exists(BackupDirectory))
                {
                    Directory.CreateDirectory(BackupDirectory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al inicializar directorio de backups: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea un backup manual de la base de datos
        /// </summary>
        /// <param name="usuario">Usuario que realiza el backup</param>
        /// <returns>Ruta del archivo de backup creado, o null si falla</returns>
        public static string CrearBackupManual(Usuario usuario = null)
        {
            try
            {
                Inicializar();

                // Nombre del archivo: backup_YYYYMMDD_HHMMSS.zip
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreArchivo = $"backup_{timestamp}.zip";
                string rutaBackup = Path.Combine(BackupDirectory, nombreArchivo);

                // Crear backup comprimido
                if (CrearBackupComprimido(rutaBackup))
                {
                    FileInfo fileInfo = new FileInfo(rutaBackup);

                    // Registrar en auditoría
                    if (usuario != null)
                    {
                        AuditLogger.RegistrarBackup(usuario, rutaBackup, fileInfo.Length);
                    }

                    return rutaBackup;
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear backup: {ex.Message}", "Error de Backup",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Crea un backup automático si es necesario según la configuración
        /// </summary>
        /// <returns>True si se creó el backup o no era necesario, False si hubo error</returns>
        public static bool CrearBackupAutomaticoSiNecesario()
        {
            try
            {
                // Obtener configuración de backup automático
                var config = ObtenerConfiguracionBackup();
                if (!config.backupAutomatico)
                {
                    return true; // No es necesario, está deshabilitado
                }

                // Verificar si ya existe un backup reciente
                var ultimoBackup = ObtenerUltimoBackup();
                if (ultimoBackup != null)
                {
                    TimeSpan tiempoTranscurrido = DateTime.Now - ultimoBackup.FechaCreacion;
                    if (tiempoTranscurrido.TotalHours < config.intervaloHoras)
                    {
                        return true; // No es necesario aún
                    }
                }

                // Crear backup automático
                Inicializar();
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string nombreArchivo = $"backup_auto_{timestamp}.zip";
                string rutaBackup = Path.Combine(BackupDirectory, nombreArchivo);

                if (CrearBackupComprimido(rutaBackup))
                {
                    FileInfo fileInfo = new FileInfo(rutaBackup);
                    AuditLogger.RegistrarBackup(null, rutaBackup, fileInfo.Length);

                    // Limpiar backups antiguos
                    LimpiarBackupsAntiguos();

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en backup automático: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restaura la base de datos desde un archivo de backup
        /// </summary>
        /// <param name="rutaBackup">Ruta del archivo de backup</param>
        /// <param name="usuario">Usuario que realiza la restauración</param>
        /// <returns>True si se restauró correctamente</returns>
        public static bool RestaurarBackup(string rutaBackup, Usuario usuario)
        {
            try
            {
                if (!File.Exists(rutaBackup))
                {
                    MessageBox.Show("El archivo de backup no existe.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // Confirmación del usuario
                var resultado = MessageBox.Show(
                    "⚠️ ADVERTENCIA: Esta acción reemplazará TODOS los datos actuales con los datos del backup.\n\n" +
                    "Se creará un backup automático de seguridad antes de continuar.\n\n" +
                    "¿Está seguro de que desea continuar?",
                    "Confirmar Restauración",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resultado != MessageBoxResult.Yes)
                {
                    return false;
                }

                // Crear backup de seguridad antes de restaurar
                string backupSeguridad = Path.Combine(BackupDirectory,
                    $"backup_pre_restauracion_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                CrearBackupComprimido(backupSeguridad);

                // Cerrar todas las conexiones a la base de datos
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Obtener ruta de la base de datos
                string dbPath = Database.GetDatabasePath();

                // Extraer el backup
                string tempDir = Path.Combine(Path.GetTempPath(), "TuTiendita_Restore_" + Guid.NewGuid());
                Directory.CreateDirectory(tempDir);

                try
                {
                    ZipFile.ExtractToDirectory(rutaBackup, tempDir);

                    // Buscar el archivo de base de datos en el backup
                    string[] dbFiles = Directory.GetFiles(tempDir, "TuTiendita.db", SearchOption.AllDirectories);
                    if (dbFiles.Length == 0)
                    {
                        MessageBox.Show("El backup no contiene un archivo de base de datos válido.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Reemplazar la base de datos actual
                    File.Copy(dbFiles[0], dbPath, true);

                    // Registrar en auditoría
                    AuditLogger.RegistrarRestauracion(usuario, rutaBackup);

                    MessageBox.Show(
                        "✓ Base de datos restaurada correctamente.\n\n" +
                        "La aplicación se reiniciará para aplicar los cambios.",
                        "Restauración Exitosa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Reiniciar la aplicación
                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Application.Current.Shutdown();

                    return true;
                }
                finally
                {
                    // Limpiar directorio temporal
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al restaurar backup: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Obtiene la lista de backups disponibles
        /// </summary>
        public static BackupInfo[] ObtenerBackupsDisponibles()
        {
            try
            {
                Inicializar();

                if (!Directory.Exists(BackupDirectory))
                {
                    return new BackupInfo[0];
                }

                var archivos = Directory.GetFiles(BackupDirectory, "*.zip")
                    .Select(ruta => new BackupInfo
                    {
                        Ruta = ruta,
                        Nombre = Path.GetFileName(ruta),
                        FechaCreacion = File.GetCreationTime(ruta),
                        TamanoBytes = new FileInfo(ruta).Length,
                        EsAutomatico = Path.GetFileName(ruta).Contains("_auto_")
                    })
                    .OrderByDescending(b => b.FechaCreacion)
                    .ToArray();

                return archivos;
            }
            catch
            {
                return new BackupInfo[0];
            }
        }

        /// <summary>
        /// Obtiene el último backup realizado
        /// </summary>
        private static BackupInfo ObtenerUltimoBackup()
        {
            var backups = ObtenerBackupsDisponibles();
            return backups.FirstOrDefault();
        }

        /// <summary>
        /// Elimina backups automáticos antiguos (mantiene solo los últimos 10)
        /// </summary>
        private static void LimpiarBackupsAntiguos()
        {
            try
            {
                var backupsAutomaticos = ObtenerBackupsDisponibles()
                    .Where(b => b.EsAutomatico)
                    .OrderByDescending(b => b.FechaCreacion)
                    .Skip(10) // Mantener los últimos 10
                    .ToList();

                foreach (var backup in backupsAutomaticos)
                {
                    try
                    {
                        File.Delete(backup.Ruta);
                    }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// Crea un archivo ZIP comprimido con la base de datos
        /// </summary>
        private static bool CrearBackupComprimido(string rutaDestino)
        {
            try
            {
                // Forzar cierre de conexiones
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string dbPath = Database.GetDatabasePath();
                if (!File.Exists(dbPath))
                {
                    return false;
                }

                // Crear directorio temporal
                string tempDir = Path.Combine(Path.GetTempPath(), "TuTiendita_Backup_" + Guid.NewGuid());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Copiar base de datos a directorio temporal
                    string tempDbPath = Path.Combine(tempDir, "TuTiendita.db");
                    File.Copy(dbPath, tempDbPath, true);

                    // Crear archivo info.txt con metadata
                    string infoPath = Path.Combine(tempDir, "info.txt");
                    File.WriteAllText(infoPath,
                        $"TuTiendita - Backup de Base de Datos\n" +
                        $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                        $"Versión: 1.0\n" +
                        $"Tamaño DB: {new FileInfo(dbPath).Length / 1024.0:N2} KB\n");

                    // Crear archivo ZIP
                    if (File.Exists(rutaDestino))
                    {
                        File.Delete(rutaDestino);
                    }

                    ZipFile.CreateFromDirectory(tempDir, rutaDestino, CompressionLevel.Optimal, false);

                    return true;
                }
                finally
                {
                    // Limpiar directorio temporal
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al crear backup comprimido: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la configuración de backup desde la base de datos
        /// </summary>
        private static (bool backupAutomatico, int intervaloHoras) ObtenerConfiguracionBackup()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    using (var cmd = new System.Data.SQLite.SQLiteCommand(
                        "SELECT BackupAutomatico, IntervaloBackupHoras FROM Configuracion WHERE Id = 1",
                        connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool backupAuto = reader.GetInt32(0) == 1;
                                int intervalo = reader.GetInt32(1);
                                return (backupAuto, intervalo);
                            }
                        }
                    }
                }
            }
            catch { }

            // Valores por defecto
            return (true, 24);
        }

        /// <summary>
        /// Abre el directorio de backups en el explorador de archivos
        /// </summary>
        public static void AbrirDirectorioBackups()
        {
            try
            {
                Inicializar();
                if (Directory.Exists(BackupDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", BackupDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir directorio: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Información de un archivo de backup
    /// </summary>
    public class BackupInfo
    {
        public string Ruta { get; set; }
        public string Nombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public long TamanoBytes { get; set; }
        public bool EsAutomatico { get; set; }

        public string TamanoFormateado
        {
            get
            {
                if (TamanoBytes < 1024)
                    return $"{TamanoBytes} B";
                else if (TamanoBytes < 1024 * 1024)
                    return $"{TamanoBytes / 1024.0:N2} KB";
                else
                    return $"{TamanoBytes / (1024.0 * 1024.0):N2} MB";
            }
        }

        public string TipoBackup => EsAutomatico ? "Automático" : "Manual";

        public string FechaFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
