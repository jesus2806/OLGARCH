using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    internal class NumeroAColorEstatusOrden : BaseConverterOneWay<int, Color>
    {
        public override Color DefaultConvertReturnValue { get; set; } = Colors.Gray;

        public override Color ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                0 => Color.FromArgb("#B5B5B5"),
                1 => Color.FromArgb("#6CAFD9"), // azul
                2 => Color.FromArgb("#F7CD82"), // naranga
                3 => Color.FromArgb("#F2C6C2"), // rosa
                4 => Color.FromArgb("#9AEBA3"), // verde
                _ => Color.FromArgb("#D2BBF2"), // purpura
            };
        }
    }
}
