using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Ingrediente")]
    public class Ingrediente : INotifyPropertyChanged
    {
        // PK local (SQLite) opcional, por si lo guardas local.
        [PrimaryKey, AutoIncrement]
        public int iIdIngredienteLocal { get; set; }

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;

        [JsonPropertyName("iCantidadEnAlmacen")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal iCantidadEnAlmacen { get; set; }

        [JsonPropertyName("iCantidadMinima")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public decimal iCantidadMinima { get; set; }

        [JsonPropertyName("sUnidad")]
        public string sUnidad { get; set; } = string.Empty;

        [JsonPropertyName("iCostoUnidad")]
        public int iCostoUnidad { get; set; }

        [JsonPropertyName("__v")]
        [Ignore]
        public int? iVersion { get; set; }

        // Útil para UI (selección)
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

        [Ignore]
        public bool bBajoStock => iCantidadEnAlmacen <= iCantidadMinima;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
