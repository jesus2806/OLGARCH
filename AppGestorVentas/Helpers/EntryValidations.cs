using System.Text.RegularExpressions;

namespace AppGestorVentas.Helpers
{
    class EntryValidations
    {
        public static bool IsOnlyLetters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Expresión regular para letras incluyendo acentos
            string pattern = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$";

            return Regex.IsMatch(input, pattern);
        }

        /// <summary>
        /// Valida si la entrada contiene únicamente números.
        /// </summary>
        /// <param name="input">La cadena a evaluar.</param>
        /// <returns>True si la cadena contiene solo dígitos; false en caso contrario.</returns>
        public static bool IsOnlyNumbers(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Expresión regular que valida que la cadena contenga únicamente dígitos
            string pattern = @"^\d+$";
            return Regex.IsMatch(input, pattern);
        }

        public static bool IsValidText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Expresión regular para letras, números, espacios, puntos y comas
            string pattern = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s.,]+$";

            return Regex.IsMatch(input, pattern);
        }


        public static bool IsValidUsuario(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Expresión regular para letras, números, espacios, puntos y comas
            string pattern = @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s.]+$";

            return Regex.IsMatch(input, pattern);
        }


        /// <summary>
        /// Valida si el nombre de archivo tiene una extensión de imagen válida.
        /// Extensiones permitidas: jpg, jpeg, png, gif, webp.
        /// </summary>
        /// <param name="fileName">El nombre o ruta del archivo.</param>
        /// <returns>True si la extensión es válida; false en caso contrario.</returns>
        public static bool HasValidImageExtension(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Expresión regular que verifica que el archivo termine en .jpg, .jpeg, .png, .gif o .webp
            string pattern = @"^.*\.(jpg|jpeg|png|gif|webp)$";
            return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase);
        }
    }


}
