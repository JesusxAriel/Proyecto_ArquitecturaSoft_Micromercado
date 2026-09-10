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

            if (!IsValid(category))
            {
                return false;
            }

            category.AdminUserId = 1;

            return categoryRepository.Add(category);
        }

        public bool Update(Category category)
        {
            Normalize(category);

            if (!IsValid(category))
            {
                return false;
            }

            category.AdminUserId = 1;

            return categoryRepository.Update(category);
        }

        public bool Delete(int id, int adminUserId)
        {
            return categoryRepository.Delete(id, adminUserId);
        }

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

        private static bool IsValid(Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return false;
            }

            if (category.Name.Length > 150)
            {
                return false;
            }

            if (!Regex.IsMatch(
                    category.Name,
                    CategoryValidation.NamePattern,
                    RegexOptions.CultureInvariant))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(category.Code))
            {
                return false;
            }

            if (category.Code.Length > 20)
            {
                return false;
            }

            if (!Regex.IsMatch(
                    category.Code,
                    CategoryValidation.CodePattern,
                    RegexOptions.CultureInvariant))
            {
                return false;
            }

            if (category.Description?.Length > 255)
            {
                return false;
            }

            if (category.Description is not null &&
                !Regex.IsMatch(
                    category.Description,
                    CategoryValidation.DescriptionPattern,
                    RegexOptions.CultureInvariant))
            {
                return false;
            }

            if (category.AisleLocation?.Length > 20)
            {
                return false;
            }

            if (category.AisleLocation is not null &&
                !Regex.IsMatch(
                    category.AisleLocation,
                    CategoryValidation.AislePattern,
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant))
            {
                return false;
            }

            return true;
        }
    }
}