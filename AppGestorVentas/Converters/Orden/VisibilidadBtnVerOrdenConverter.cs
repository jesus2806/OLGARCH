using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    class VisibilidadBtnVerOrdenConverter : BaseConverterOneWay<int, bool>
    {
        public override bool DefaultConvertReturnValue { get; set; } = false;

        public override bool ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                4 => false, // Si esta en estado "Entregada" se mostrará el boton pagada
                5 => false, // Si esta en estado "Pagada" se mostrará el boton pagada
                _ => true
            };
        }
    }
}
