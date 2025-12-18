using System.Globalization;

namespace AppGestorVentas.Converters
{
    /// <summary>
    /// Convierte un porcentaje (0-100) a decimal (0-1) para ProgressBar
    /// </summary>
    public class PercentToDecimalConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percent)
            {
                return percent / 100.0;
            }
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double decimalValue)
            {
                return decimalValue * 100.0;
            }
            return 0.0;
        }
    }
}
