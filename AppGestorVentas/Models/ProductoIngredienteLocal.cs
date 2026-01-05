using SQLite;

namespace AppGestorVentas.Models
{
    [Table("tb_ProductoIngrediente")]
    public class ProductoIngredienteLocal
    {
        [PrimaryKey, AutoIncrement]
        public int iId { get; set; }

        [Indexed]
        public string sIdMongoDBProducto { get; set; } = string.Empty;

        [Indexed]
        public string sIdIngrediente { get; set; } = string.Empty;

        public decimal iCantidadUso { get; set; }
    }
}
