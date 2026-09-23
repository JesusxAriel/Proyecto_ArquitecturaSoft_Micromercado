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

        public List<Category> GetActive()
        {
            return categoryRepository.GetActive();
        }

        public Category? GetById(int id)
        {
            return categoryRepository.GetById(id);
        }

        public bool Create(Category category)
        {
            Normalize(category);
            Validate(category);

            category.AdminUserId = 1;

            return categoryRepository.Add(category);
        }

        public bool Update(Category category)
        {
            Normalize(category);
            Validate(category);

            category.AdminUserId = 1;

            return categoryRepository.Update(category);
        }

        public bool Delete(int id, int adminUserId)
        {
            return categoryRepository.Delete(id, adminUserId);
        }

        public Task<Category?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GetById(id));

        public Task<int> CreateAsync(
            Category dto,
            CancellationToken cancellationToken = default)
        {
            var created = Create(dto);
            return Task.FromResult(created ? dto.Id : 0);
        }

        public Task<bool> UpdateAsync(
            Category dto,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Update(dto));

        public Task<bool> SoftDeleteAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Delete(id, 1));

        private static void Normalize(Category category)
        {
            category.Name = category.Name.Trim();
            category.Code = category.Code.Trim();

            category.Description =
                string.IsNullOrWhiteSpace(category.Description)
                    ? null
                    : category.Description.Trim();

            category.AisleLocation =
                string.IsNullOrWhiteSpace(category.AisleLocation)
                    ? null
                    : category.AisleLocation.Trim();
        }

        private static void Validate(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                throw new ArgumentException("El nombre de la categoría es obligatorio.");
            }

            if (category.Name.Length > 150)
            {
                throw new ArgumentException("El nombre no puede exceder los 150 caracteres.");
            }

            if (!Regex.IsMatch(
                    category.Name,
                    CategoryValidation.NamePattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(CategoryValidation.NameMessage);
            }

            if (string.IsNullOrWhiteSpace(category.Code))
            {
                throw new ArgumentException("El código de la categoría es obligatorio.");
            }

            if (category.Code.Length > 20)
            {
                throw new ArgumentException("El código no puede exceder los 20 caracteres.");
            }

            if (!Regex.IsMatch(
                    category.Code,
                    CategoryValidation.CodePattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(CategoryValidation.CodeMessage);
            }

            if (category.Description?.Length > 255)
            {
                throw new ArgumentException("La descripción no puede exceder los 255 caracteres.");
            }

            if (category.Description is not null &&
                !Regex.IsMatch(
                    category.Description,
                    CategoryValidation.DescriptionPattern,
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(CategoryValidation.DescriptionMessage);
            }

            if (category.AisleLocation?.Length > 20)
            {
                throw new ArgumentException("La ubicación no puede exceder los 20 caracteres.");
            }

            if (category.AisleLocation is not null &&
                !Regex.IsMatch(
                    category.AisleLocation,
                    CategoryValidation.AislePattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                throw new ArgumentException(CategoryValidation.AisleMessage);
            }
        }
    }
}