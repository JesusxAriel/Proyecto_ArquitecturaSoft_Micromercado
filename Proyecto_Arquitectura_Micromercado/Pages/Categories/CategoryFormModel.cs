using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public abstract class CategoryFormModel : PageModel
    {
        protected readonly ICategoryService CategoryService;

        [BindProperty]
        public Category Category { get; set; } = new Category();

        public string ErrorMessage { get; set; } = string.Empty;

        protected CategoryFormModel(ICategoryService categoryService)
        {
            CategoryService = categoryService;
        }
    }
}