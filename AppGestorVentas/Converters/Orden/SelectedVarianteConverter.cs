using AppGestorVentas.Models;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    public class SelectedVarianteConverter : IValueConverter
    {
        /// <summary>
        /// Recibe el objeto OrdenProducto y devuelve el texto de la variante seleccionada.
        /// Se asume que aVariantes es una lista de objetos que tienen la propiedad sVariante.
        /// </summary>
        /// <param name="value">El objeto OrdenProducto</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns>La cadena correspondiente a la variante seleccionada o cadena vacía</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OrdenProducto ordenProducto &&
                ordenProducto.aVariantes != null &&
                ordenProducto.aVariantes.Count > 0)
            {
                int index = ordenProducto.iIndexVarianteSeleccionada;
                if (index >= 0 && index < ordenProducto.aVariantes.Count)
                {
                    return ordenProducto.aVariantes[index].sVariante;
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
