using CommunityToolkit.Maui.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Converters.Orden
{
    internal class NumeroABoolSubOrden : BaseConverterOneWay<int, bool>
    {
        public override bool DefaultConvertReturnValue { get; set; } = false;

        public override bool ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                1 => true,
                2 => false,
                _ => false
            };
        }
    }
}
