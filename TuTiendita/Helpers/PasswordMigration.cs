using System;
using System.Data.SQLite;
using System.Windows;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Utilidad para migrar contraseñas en texto plano a hashes BCrypt
    /// </summary>
    public static class PasswordMigration
    {
        /// <summary>
        /// Verifica si hay usuarios con contraseñas en texto plano y los migra a BCrypt
        /// </summary>
        /// <returns>Número de contraseñas migradas</returns>
        public static int MigrarPasswordsABCrypt()
        {
            int passwordsMigradas = 0;

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();

                    // Obtener todos los usuarios
                    string querySelect = "SELECT Id, Nombre, Contrasena FROM Usuarios";
                    var usuariosAMigrar = new System.Collections.Generic.List<(int id, string nombre, string password)>();

                    using (var cmd = new SQLiteCommand(querySelect, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = reader.GetInt32(0);
                                string nombre = reader.GetString(1);
                                string password = reader.GetString(2);

                                // Verificar si la contraseña ya es un hash BCrypt
                                if (!SecurityHelper.IsBCryptHash(password))
                                {
                                    usuariosAMigrar.Add((id, nombre, password));
                                }
                            }
                        }
                    }

                    // Migrar contraseñas en texto plano a BCrypt
                    if (usuariosAMigrar.Count > 0)
                    {
                        string queryUpdate = "UPDATE Usuarios SET Contrasena = @Password WHERE Id = @Id";

                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                foreach (var usuario in usuariosAMigrar)
                                {
                                    // Generar hash BCrypt de la contraseña en texto plano
                                    string passwordHash = SecurityHelper.HashPassword(usuario.password);

                                    using (var cmd = new SQLiteCommand(queryUpdate, connection))
                                    {
                                        cmd.Parameters.AddWithValue("@Password", passwordHash);
                                        cmd.Parameters.AddWithValue("@Id", usuario.id);
                                        cmd.ExecuteNonQuery();
                                    }

                                    passwordsMigradas++;
                                    System.Diagnostics.Debug.WriteLine($"Password migrada para usuario: {usuario.nombre} (ID: {usuario.id})");
                                }

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception($"Error durante la migración: {ex.Message}");
                            }
                        }
                    }
                }

                return passwordsMigradas;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en migración de passwords: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ejecuta la migración con retroalimentación visual al usuario
        /// </summary>
        /// <param name="silencioso">Si es verdadero, no muestra mensajes cuando no hay nada que migrar</param>
        public static void EjecutarMigracionConFeedback(bool silencioso = true)
        {
            try
            {
                int passwordsMigradas = MigrarPasswordsABCrypt();

                if (passwordsMigradas > 0)
                {
                    MessageBox.Show(
                        $"✓ Migración de seguridad completada\n\n" +
                        $"{passwordsMigradas} contraseña(s) fueron encriptadas con BCrypt.\n\n" +
                        $"Esto mejora significativamente la seguridad de tu sistema.",
                        "Actualización de Seguridad",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    System.Diagnostics.Debug.WriteLine($"Migración completada: {passwordsMigradas} passwords migradas");
                }
                else if (!silencioso)
                {
                    MessageBox.Show(
                        "✓ Todas las contraseñas ya están encriptadas con BCrypt.\n\nNo se requiere migración.",
                        "Sistema Actualizado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error durante la migración de contraseñas:\n\n{ex.Message}\n\n" +
                    $"Por favor contacte al administrador del sistema.",
                    "Error de Migración",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Error en migración: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si todos los usuarios tienen contraseñas BCrypt
        /// </summary>
        /// <returns>True si todas las contraseñas están encriptadas</returns>
        public static bool VerificarTodasPasswordsBCrypt()
        {
            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Contrasena FROM Usuarios";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string password = reader.GetString(0);
                                if (!SecurityHelper.IsBCryptHash(password))
                                {
                                    return false; // Encontró una contraseña en texto plano
                                }
                            }
                        }
                    }
                }

                return true; // Todas las contraseñas son BCrypt
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando passwords: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas sobre el estado de las contraseñas
        /// </summary>
        public static (int totalUsuarios, int passwordsBCrypt, int passwordsPlainText) ObtenerEstadisticas()
        {
            int total = 0;
            int bcrypt = 0;
            int plaintext = 0;

            try
            {
                using (var connection = Database.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT Contrasena FROM Usuarios";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                total++;
                                string password = reader.GetString(0);

                                if (SecurityHelper.IsBCryptHash(password))
                                {
                                    bcrypt++;
                                }
                                else
                                {
                                    plaintext++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
            }

            return (total, bcrypt, plaintext);
        }
    }
}
