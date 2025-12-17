using System.Globalization;

namespace AppGestorVentas.Converters
{
    internal class BoolToStringConverter : IValueConverter
    {
        // ConverterParameter en XAML => "Quitar|Seleccionar"
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolVal && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return boolVal ? parts[0] : parts[1];
                }
            }
            return "Seleccionar";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
