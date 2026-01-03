using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AppGestorVentas.Helpers;

namespace AppGestorVentas.Models
{
    public class DiaEsquema : INotifyPropertyChanged
    {
        private string _sDia = string.Empty;
        private string _dValor = ""; // string para evitar crash al borrar en Entry

        [JsonPropertyName("sDia")]
        public string sDia
        {
            get => _sDia;
            set { _sDia = value ?? string.Empty; OnPropertyChanged(); }
        }

        [JsonPropertyName("dValor")]
        [JsonConverter(typeof(Decimal128StringJsonConverter))]
        public string dValor
        {
            get => _dValor;
            set { _dValor = value ?? ""; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
