using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Categories
{
    public class Category
    {
        public int Id { get; set; }

        [Display(Name = "Nombre de la categoría")]
        [Required(ErrorMessage = "Campo obligatorio.")]
        [StringLength(
            150,
            ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
        [CategoryName]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [StringLength(
            255,
            ErrorMessage = "La descripción no puede exceder los 255 caracteres.")]
        [CategoryDescription]
        public string? Description { get; set; }

        [Display(Name = "Código")]
        [Required(ErrorMessage = "Campo obligatorio.")]
        [StringLength(
            20,
            ErrorMessage = "El código no puede exceder los 20 caracteres.")]
        [CategoryCode]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Ubicación en pasillo")]
        [StringLength(
            20,
            ErrorMessage = "La ubicación no puede exceder los 20 caracteres.")]
        [AisleLocation]
        public string? AisleLocation { get; set; }

        public bool IsActive { get; set; } = true;

        public int AdminUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}