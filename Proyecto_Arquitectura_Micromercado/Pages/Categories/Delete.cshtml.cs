using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        private readonly ICategoryService categoryService;

        [BindProperty]
        public Category Category { get; set; } = new Category();
        [BindProperty]
        public string ConfirmationName { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public DeleteModel(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        public IActionResult OnGet(int id)
        {
            Category? category =
                categoryService.GetById(id);

            if (category == null)
            {
                return NotFound();
            }

            Category = category;

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            if (!string.Equals(Category.Name, ConfirmationName, StringComparison.Ordinal))
            {
                ErrorMessage = "El nombre de confirmación no coincide exactamente.";
                Category = categoryService.GetById(id) ?? new Category { Id = id };
                return Page();
            }

            try
            {
                int adminUserId = 1;

                bool deleted =
                    categoryService.Delete(id, adminUserId);

                if (!deleted)
                {
                    ErrorMessage =
                        "No se pudo eliminar la categoría.";

                    Category? category =
                        categoryService.GetById(id);

                    if (category != null)
                    {
                        Category = category;
                    }

                    return Page();
                }

                return RedirectToPage("/Categories/Index");
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Ocurrió un error al eliminar la categoría.";

                return Page();
            }
        }
    }
}