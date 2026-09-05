namespace Proyecto_Arquitectura_Micromercado.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public double Precio { get; set; }
        public int Stock { get; set; }
        public int IdProveedor { get; set; }
        public int IdUsuarioAdmi { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }
}