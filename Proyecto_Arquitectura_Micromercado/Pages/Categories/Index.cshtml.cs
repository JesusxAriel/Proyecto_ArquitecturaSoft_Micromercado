using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryService categoryService;

        public List<Category> Categories { get; set; } =
            new List<Category>();

        public string ErrorMessage { get; set; } = string.Empty;

        public IndexModel(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        public void OnGet()
        {
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                Categories = categoryService.GetActive();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Error al cargar las categorías: " + ex.Message;
            }
        }
    }
}