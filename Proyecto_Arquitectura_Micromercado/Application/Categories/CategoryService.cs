using System.Globalization;
using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories
{
    public class CategoryService : ICategoryService
    {
        private const int MaxCodeAttempts = 3;

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
            await EnsureNameIsUniqueAsync(category, cancellationToken);

            category.AdminUserId = 1;

            for (int attempt = 1; ; attempt++)
            {
                category.Code = await GenerateCodeAsync(
                    category.Name,
                    cancellationToken);

                try
                {
                    return await categoryRepository.CreateAsync(
                        category,
                        cancellationToken);
                }
                catch (DuplicateCategoryCodeException)
                    when (attempt < MaxCodeAttempts)
                {
                    // Otra solicitud tomó el mismo código entre la lectura y el INSERT:
                    // se vuelve a generar con la lista actualizada.
                }
            }
        }

        public async Task<bool> UpdateAsync(
            Category category,
            CancellationToken cancellationToken = default)
        {
            Category? current = await categoryRepository.GetByIdAsync(
                category.Id,
                cancellationToken);

            if (current is null)
            {
                return false;
            }

            Normalize(category);
            Validate(category);
            await EnsureNameIsUniqueAsync(category, cancellationToken);

            // El código se asigna al crear y no cambia al editar el nombre.
            category.Code = current.Code;
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

        // Al crear Id vale 0, así que no excluye a nadie; al editar excluye el propio registro.
        private async Task EnsureNameIsUniqueAsync(
            Category category,
            CancellationToken cancellationToken)
        {
            if (await categoryRepository.ExistsNameAsync(
                    category.Name,
                    category.Id,
                    cancellationToken))
            {
                throw new DuplicateCategoryNameException();
            }
        }

        private async Task<string> GenerateCodeAsync(
            string name,
            CancellationToken cancellationToken)
        {
            if (!CategoryCodeGenerator.HasEnoughLetters(name))
            {
                throw new ArgumentException(
                    CategoryValidation.CodeNotEnoughLettersMessage);
            }

            var existingCodes = new HashSet<string>(
                await categoryRepository.GetAllCodesAsync(cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            return CategoryCodeGenerator.Generate(name, existingCodes)
                ?? throw new ArgumentException(
                    CategoryValidation.CodeExhaustedMessage);
        }

        private static void Normalize(Category category)
        {
            category.Name = ToTitleCase(category.Name);

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