using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    class VisibilidadBtnPagarConverter : BaseConverterOneWay<int, bool>
    {
        public override bool DefaultConvertReturnValue { get; set; } = false;

        public override bool ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                4 => true, // Si esta en estado "Entregada" se mostrará el boton pagada
                _ => false
            };
        }
    }
}
