using System;
using BCrypt.Net;

namespace TuTiendita.Helpers
{
    /// <summary>
    /// Helper para funciones de seguridad: encriptación de contraseñas, validación, etc.
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Encripta una contraseña usando BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Hash BCrypt de la contraseña</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía");

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Verifica si una contraseña coincide con un hash BCrypt
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <param name="hash">Hash BCrypt almacenado</param>
        /// <returns>True si coincide, False si no</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un string es un hash BCrypt válido
        /// </summary>
        /// <param name="hash">String a verificar</param>
        /// <returns>True si es un hash BCrypt, False si es texto plano</returns>
        public static bool IsBCryptHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                return false;

            // Los hashes BCrypt empiezan con $2a$, $2b$, $2x$ o $2y$
            return hash.StartsWith("$2a$") || hash.StartsWith("$2b$") ||
                   hash.StartsWith("$2x$") || hash.StartsWith("$2y$");
        }

        /// <summary>
        /// Valida la complejidad de una contraseña
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Tuple con (esValida, mensajeError)</returns>
        public static (bool esValida, string mensajeError) ValidarComplejidadPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "La contraseña no puede estar vacía");

            if (password.Length < 6)
                return (false, "La contraseña debe tener al menos 6 caracteres");

            if (password.Length > 100)
                return (false, "La contraseña no puede exceder 100 caracteres");

            // Opcional: requerir mayúsculas, números, etc.
            // Por ahora solo longitud mínima

            return (true, string.Empty);
        }

        /// <summary>
        /// Verifica password permitiendo tanto hashes BCrypt como texto plano (para migración)
        /// </summary>
        /// <param name="password">Contraseña ingresada</param>
        /// <param name="storedPassword">Contraseña almacenada (puede ser hash o texto plano)</param>
        /// <param name="esHash">Indica si la contraseña almacenada es un hash</param>
        /// <returns>True si coincide</returns>
        public static bool VerifyPasswordCompat(string password, string storedPassword, out bool esHash)
        {
            esHash = IsBCryptHash(storedPassword);

            if (esHash)
            {
                return VerifyPassword(password, storedPassword);
            }
            else
            {
                // Comparación directa para contraseñas en texto plano (legacy)
                return password == storedPassword;
            }
        }
    }
}
