using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    class VisibilidadBtnConfirmarConverter : BaseConverterOneWay<int, bool>
    {
        public override bool DefaultConvertReturnValue { get; set; } = false;

        public override bool ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                3 => true, // Si esta en estado "Preparada" se mostrará el boton confirmar
                _ => false
            };
        }
    }
}
