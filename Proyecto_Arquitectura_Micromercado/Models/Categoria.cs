using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(20, ErrorMessage = "El código no debe superar los 20 caracteres.")]
        public string Codigo { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El pasillo de ubicación no debe superar los 20 caracteres.")]
        public string? PasilloUbicacion { get; set; }

        public bool EstaActivo { get; set; } = true;
        public int IdUsuarioAdmin { get; set; } = 1;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }
}