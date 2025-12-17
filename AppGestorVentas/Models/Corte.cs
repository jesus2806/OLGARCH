namespace AppGestorVentas.Models
{
    public class Corte
    {
        public DateTime dFechaCorte { get; set; }
        public decimal dTotalCostoPublico { get; set; }
        public decimal dTotalCostoReal { get; set; }
        public decimal dTotalGanancia { get; set; }
        public decimal dTotalEfectivo { get; set; }
        public decimal dTotalTransferencia { get; set; }
    }
}
