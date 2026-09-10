using System.ComponentModel.DataAnnotations;

namespace Proyecto_Arquitectura_Micromercado.Domain.Categories
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(
            150,
            ErrorMessage = "Category name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        private string? description;

        [StringLength(
            255,
            ErrorMessage = "Description cannot exceed 255 characters.")]
        public string? Description
        {
            get => description;
            set => description = CapitalizeFirstLetter(value);
        }

        [Required(ErrorMessage = "Category code is required.")]
        [StringLength(
            20,
            ErrorMessage = "Category code cannot exceed 20 characters.")]
        public string Code { get; set; } = string.Empty;

        [StringLength(
            20,
            ErrorMessage = "Aisle location cannot exceed 20 characters.")]
        public string? AisleLocation { get; set; }

        public bool IsActive { get; set; } = true;

        public int AdminUserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        private static string? CapitalizeFirstLetter(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            return char.ToUpper(trimmed[0], System.Globalization.CultureInfo.CurrentCulture)
                + trimmed[1..];
        }
    }
}