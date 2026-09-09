namespace Proyecto_Arquitectura_Micromercado.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string NombreEmpresa { get; set; } = string.Empty;
        public string NumeroEmpresa { get; set; } = string.Empty;
        public string? CorreoReferencia { get; set; }
        public bool EsAutogestionado { get; set; }
        public bool EstaActivo { get; set; } = true;
        public int IdUsuarioAdmin { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }
}