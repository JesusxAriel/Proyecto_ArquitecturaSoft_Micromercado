using System.Globalization;
using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            this.categoryRepository = categoryRepository;
        }

        public async Task<IReadOnlyList<Category>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await categoryRepository.GetAllAsync(
                cancellationToken);
        }

        public async Task<Category?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await categoryRepository.GetByIdAsync(
                id,
                cancellationToken);
        }

        public async Task<int> CreateAsync(
            Category category,
            CancellationToken cancellationToken = default)
        {
            Normalize(category);
            Validate(category);

            category.AdminUserId = 1;

            return await categoryRepository.CreateAsync(
                category,
                cancellationToken);
        }

        public async Task<bool> UpdateAsync(
            Category category,
            CancellationToken cancellationToken = default)
        {
            Normalize(category);
            Validate(category);

            category.AdminUserId = 1;

            return await categoryRepository.UpdateAsync(
                category,
                cancellationToken);
        }

        public async Task<bool> SoftDeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await categoryRepository.SoftDeleteAsync(
                id,
                cancellationToken);
        }

        private static void Normalize(Category category)
        {
            category.Name = ToTitleCase(category.Name);

            category.Code = NormalizeCategoryCode(
                category.Code);

            category.Description =
                string.IsNullOrWhiteSpace(category.Description)
                    ? null
                    : NormalizeSpaces(category.Description);

            category.AisleLocation =
                string.IsNullOrWhiteSpace(category.AisleLocation)
                    ? null
                    : NormalizeSpaces(category.AisleLocation);
        }

        private static string NormalizeSpaces(string value)
        {
            return Regex.Replace(
                value.Trim(),
                @"\s+",
                " ");
        }

        private static string ToTitleCase(string value)
        {
            string normalized = NormalizeSpaces(value);

            return CultureInfo
                .CurrentCulture
                .TextInfo
                .ToTitleCase(normalized.ToLower());
        }

        private static string NormalizeCategoryCode(string value)
        {
            string code = value.Trim().ToUpperInvariant();

            if (code.StartsWith("CAT-"))
            {
                code = code[4..];
            }

            code = Regex.Replace(code, @"\s+", "");

            return $"CAT-{code}";
        }

        private static void Validate(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException(
                    "El nombre de la categoría es obligatorio.");
            }

            if (category.Name.Length > 150)
            {
                throw new ArgumentException(
                    "El nombre no puede exceder los 150 caracteres.");
            }

            if (!Regex.IsMatch(
                    category.Name,
                    CategoryValidation.NamePattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(
                    CategoryValidation.NameMessage);
            }

            if (string.IsNullOrWhiteSpace(category.Code))
            {
                throw new ArgumentException(
                    "El código de la categoría es obligatorio.");
            }

            if (category.Code.Length > 20)
            {
                throw new ArgumentException(
                    "El código no puede exceder los 20 caracteres.");
            }

            if (!Regex.IsMatch(
                    category.Code,
                    CategoryValidation.CodePattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(
                    CategoryValidation.CodeMessage);
            }

            if (category.Description?.Length > 255)
            {
                throw new ArgumentException(
                    "La descripción no puede exceder los 255 caracteres.");
            }

            if (category.Description is not null &&
                !Regex.IsMatch(
                    category.Description,
                    CategoryValidation.DescriptionPattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(
                    CategoryValidation.DescriptionMessage);
            }

            if (string.IsNullOrWhiteSpace(category.AisleLocation))
            {
                throw new ArgumentException(
                    "El pasillo es obligatorio.");
            }

            if (category.AisleLocation?.Length > 20)
            {
                throw new ArgumentException(
                    "La ubicación no puede exceder los 20 caracteres.");
            }

            if (category.AisleLocation is not null &&
                !Regex.IsMatch(
                    category.AisleLocation,
                    CategoryValidation.AislePattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(
                    CategoryValidation.AisleMessage);
            }
        }
    }
}