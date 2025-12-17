using CommunityToolkit.Maui.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGestorVentas.Converters.Orden
{
    internal class NumeroAEstatusOrden : BaseConverterOneWay<int, string>
    {
        public override string DefaultConvertReturnValue { get; set; } = "Inválido";

        public override string ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                0 => "Pendiente",
                1 => "Confirmada",
                2 => "En preparación",
                3 => "Preparada",
                4 => "Entregada",
                5 => "Pagada",
                _ => "Inválido"
            };
        }
    }
}
