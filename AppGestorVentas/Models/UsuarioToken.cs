namespace AppGestorVentas.Models
{
    public class UsuarioToken
    {
        public string sIdUsuarioMongoDB { get; set; } = string.Empty;
        public string sNombreUsuario { get; set; } = string.Empty;
        public string sUsuario { get; set; } = string.Empty;
        public int iRol { get; set; }
        public string sTokenAcceso { get; set; } = string.Empty;
    }
}
