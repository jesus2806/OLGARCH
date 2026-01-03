using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_Usuario")]
    public class Usuario : INotifyPropertyChanged
    {
        // Clave primaria local para control interno (SQLite).
        [PrimaryKey, AutoIncrement]
        public int iIdUsuario { get; set; }

        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        [JsonPropertyName("sNombre")]
        public string sNombre { get; set; } = string.Empty;

        [JsonPropertyName("sApellidoPaterno")]
        public string sApellidoPaterno { get; set; } = string.Empty;

        [JsonPropertyName("sApellidoMaterno")]
        public string sApellidoMaterno { get; set; } = string.Empty;

        [JsonPropertyName("sUsuario")]
        public string sUsuario { get; set; } = string.Empty;

        [JsonPropertyName("sPassword")]
        public string sPassword { get; set; } = string.Empty;

        [JsonPropertyName("aEsquemas")]
        [Ignore]
        public List<string> aEsquemas { get; set; } = new();

        [JsonPropertyName("iRol")]
        public int iRol { get; set; }

        // Campo extra que aparece en algunos elementos del JSON (por ejemplo, "__v").
        [JsonPropertyName("__v")]
        [Ignore]
        public int? iVersion { get; set; }

        private bool _isSeleccionada;

        [Ignore]
        public bool isSeleccionada
        {
            get => _isSeleccionada;
            set
            {
                if (_isSeleccionada != value)
                {
                    _isSeleccionada = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName)
            );
        }
    }
}
