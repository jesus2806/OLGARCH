using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Converters
{
    public class InvertBoolConverter : IValueConverter
    {
        // Convierte true -> false, false -> true
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool bVal)
            {
                return !bVal;
            }
            return false; // si no era bool, regresa false.
        }

        // No se suele necesitar convertir de regreso, pero se define por la interfaz
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
