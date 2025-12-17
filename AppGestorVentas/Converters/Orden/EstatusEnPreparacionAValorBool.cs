using CommunityToolkit.Maui.Converters;
using System.Globalization;

namespace AppGestorVentas.Converters.Orden
{
    class EstatusEnPreparacionAValorBool : BaseConverterOneWay<int, bool>
    {
        public override bool DefaultConvertReturnValue { get; set; } = false;

        public override bool ConvertFrom(int value, CultureInfo? culture)
        {
            return value switch
            {
                0 => true, // Si esta en estado "Pendiente" se mostrará el boton actualizar
                1 => true, // Si esta en estado "Confirmada" se mostrará el boton actualizar
                //NUEVAS 
                2 => true, // Si esta en estado "En preparación" se mostrará el boton actualizar
                3 => true, // Si esta en estado "Preparada" se mostrará el boton actualizar
                4 => true, // Si esta en estado "Entregada" se mostrará el boton actualizar
                //NUEVAS 
                _ => false
            };
        }
    }
}
