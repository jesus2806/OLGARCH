using ESCPOS_NET.Emitters.BaseCommandValues;
using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Producto")]
    public class Producto : INotifyPropertyChanged
    {
        // Clave primaria local (SQLite), opcional si usas persistencia local
        [PrimaryKey, AutoIncrement]
        public int iIdProducto { get; set; }

        [JsonPropertyName("_id")]
        [Column("sIdMongo")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;

        [JsonPropertyName("iCostoReal")]
        public decimal iCostoReal { get; set; }

        [JsonPropertyName("iCostoPublico")]
        public decimal iCostoPublico { get; set; }

        [JsonPropertyName("imagenes")]
        [Ignore]
        public List<Imagen> aImagenes { get; set; } = new();

        public string? ImagenPrincipalUrl => aImagenes.FirstOrDefault()?.sURLImagen;

        [JsonPropertyName("aVariantes")]
        [Ignore]
        public List<Variante> aVariantes { get; set; } = new();

        [JsonPropertyName("aIngredientes")]
        [Ignore]
        public List<ProductoIngrediente> aIngredientes { get; set; } = new();

        [JsonPropertyName("iTipoProducto")]
        public int iTipoProducto { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime createdAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime updatedAt { get; set; }

        [JsonPropertyName("__v")]
        public int __v { get; set; }

        [Ignore]
        public string DescripcionPrincipal => aVariantes?.FirstOrDefault()?.sVariante ?? "";

        [Ignore]
        public string ImagenPrincipalSafe => ImagenPrincipalUrl ?? "";

        private bool _isSeleccionado;
        [Ignore]
        public bool isSeleccionado
        {
            get => _isSeleccionado;
            set
            {
                if (_isSeleccionado != value)
                {
                    _isSeleccionado = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }




}
