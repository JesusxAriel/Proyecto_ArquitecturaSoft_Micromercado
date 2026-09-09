using Microsoft.AspNetCore.Mvc;
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

        public IActionResult OnPostDelete(int id)
        {
            try
            {
                int adminUserId = 1;

                bool deleted =
                    categoryService.Delete(id, adminUserId);

                if (!deleted)
                {
                    ErrorMessage =
                        "No se pudo eliminar la categoría.";

                    LoadCategories();
                    return Page();
                }

                return RedirectToPage("/Categories/Index");
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Ocurrió un error al eliminar la categoría.";

                LoadCategories();
                return Page();
            }
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