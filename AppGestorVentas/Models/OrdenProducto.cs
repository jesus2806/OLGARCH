using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppGestorVentas.Models
{
    [Table("tb_OrdenProducto")]
    public partial class OrdenProducto : ObservableObject
    {
        /// <summary>
        /// ID local único (UUID) para sincronización offline-first
        /// </summary>
        [PrimaryKey]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID de MongoDB (se asigna después de sincronizar)
        /// </summary>
        [JsonPropertyName("_id")]
        public string sIdMongo { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el producto ha sido sincronizado con el backend
        /// </summary>
        public bool bSincronizado { get; set; } = false;

        /// <summary>
        /// Indica si hay cambios pendientes de sincronizar
        /// </summary>
        public bool bTieneCambiosPendientes { get; set; } = false;

        /// <summary>
        /// ID local de la orden a la que pertenece (para relaciones offline)
        /// </summary>
        public string sIdOrdenLocal { get; set; } = string.Empty;

        /// <summary>
        /// ID de MongoDB de la orden
        /// </summary>
        public string sIdOrdenMongoDB { get; set; } = string.Empty;

        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoReal { get; set; }
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;
        public string sIndicaciones { get; set; } = string.Empty;
        public int iIndexVarianteSeleccionada { get; set; }

        #region SERIALIZACIÓN JSON PARA SQLITE

        // Campo que SQLite almacena (JSON string)
        public string sVariantesJson { get; set; } = "[]";

        [JsonPropertyName("aVariantes")]
        [Ignore]
        public List<Variante> aVariantes
        {
            get => string.IsNullOrEmpty(sVariantesJson)
                ? new List<Variante>()
                : JsonSerializer.Deserialize<List<Variante>>(sVariantesJson) ?? new List<Variante>();
            set => sVariantesJson = JsonSerializer.Serialize(value ?? new List<Variante>());
        }

        // Campo que SQLite almacena (JSON string)
        public string sExtrasJson { get; set; } = "[]";

        [Ignore]
        public List<ExtraOrdenProducto> aExtras
        {
            get => string.IsNullOrEmpty(sExtrasJson)
                ? new List<ExtraOrdenProducto>()
                : JsonSerializer.Deserialize<List<ExtraOrdenProducto>>(sExtrasJson) ?? new List<ExtraOrdenProducto>();
            set => sExtrasJson = JsonSerializer.Serialize(value ?? new List<ExtraOrdenProducto>());
        }

        // Campo que SQLite almacena (JSON string)
        public string sConsumosJson { get; set; } = "[]";

        [Ignore]
        [JsonPropertyName("aConsumos")]
        public List<Consumo> aConsumos
        {
            get => string.IsNullOrEmpty(sConsumosJson)
                ? new List<Consumo>()
                : JsonSerializer.Deserialize<List<Consumo>>(sConsumosJson) ?? new List<Consumo>();
            set => sConsumosJson = JsonSerializer.Serialize(value ?? new List<Consumo>());
        }

        #endregion

        /// <summary>
        /// Campo privado para la cantidad
        /// </summary>
        private int _iCantidad = 1;
        
        /// <summary>
        /// Cantidad del producto - notifica cambios a la UI
        /// </summary>
        [JsonPropertyName("iCantidad")]
        public int iCantidad 
        { 
            get => _iCantidad;
            set
            {
                if (SetProperty(ref _iCantidad, value))
                {
                    // También notificar propiedades calculadas que dependen de cantidad
                    OnPropertyChanged(nameof(iTotalGeneralPublicoOrdenProducto));
                }
            }
        }
        
        /// <summary>
        /// Indica si el producto tiene extras asociados
        /// </summary>
        [JsonPropertyName("bTieneExtras")]
        public bool bTieneExtras { get; set; } = false;

        public decimal iTotalRealExtrasOrden { get; set; }
        public decimal iTotalPublicoExtrasOrden { get; set; }
        public decimal iTotalGeneralRealOrdenProducto { get; set; }
        public decimal iTotalGeneralPublicoOrdenProducto { get; set; }
        public int iTipoProducto { get; set; }

        [ObservableProperty]
        [property: Ignore]
        private bool isExpanded;

        /// <summary>
        /// Obtiene el ID efectivo (MongoDB si existe, sino el local)
        /// </summary>
        [Ignore]
        public string IdEfectivo => !string.IsNullOrEmpty(sIdMongo) ? sIdMongo : sIdLocal;

        /// <summary>
        /// Obtiene el ID de orden efectivo (MongoDB si existe, sino el local)
        /// </summary>
        [Ignore]
        public string IdOrdenEfectivo => !string.IsNullOrEmpty(sIdOrdenMongoDB) ? sIdOrdenMongoDB : sIdOrdenLocal;

        /// <summary>
        /// Guarda los cambios de las listas al JSON (llamar antes de SaveItemAsync)
        /// Este método ya no es necesario porque las propiedades serializan automáticamente,
        /// pero se mantiene por compatibilidad.
        /// </summary>
        public void SerializarListas()
        {
            // Las propiedades ahora serializan automáticamente en el setter
            // Este método se mantiene vacío por compatibilidad
        }
    }

    [Table("tb_ExtraOrdenProducto")]
    public class ExtraOrdenProducto
    {
        [PrimaryKey]
        public string sIdLocal { get; set; } = Guid.NewGuid().ToString();

        public string sIdOrdenProductoLocal { get; set; } = string.Empty;

        [JsonPropertyName("_id")]
        public string sIdExtra { get; set; } = string.Empty;
        
        public string sNombre { get; set; } = string.Empty;
        public decimal iCostoReal { get; set; } 
        public decimal iCostoPublico { get; set; }
        public string sURLImagen { get; set; } = string.Empty;
    }
}
