using CommunityToolkit.Maui.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Converters.Orden
{
    internal class NumeroATipoPagoConverter : BaseConverterOneWay<int, string>
    {
        public override string DefaultConvertReturnValue { get; set; } = "Inválido";

        public override string ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                1 => "Efectivo",
                2 => "Transferencia",
                3 => "Mixto",
                _ => "Inválido"
            };
        }
    }
}
