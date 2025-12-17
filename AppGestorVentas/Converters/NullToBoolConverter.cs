using System.Globalization;

namespace AppGestorVentas.Converters
{
    internal class NullToBoolConverter : IValueConverter
    {
        // Retorna true si el valor NO es null, false si es null
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
