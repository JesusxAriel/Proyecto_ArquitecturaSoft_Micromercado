using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly ICategoryService categoryService;

        [BindProperty]
        public Category Category { get; set; } = new Category();

        public string ErrorMessage { get; set; } = string.Empty;

        public EditModel(ICategoryService categoryService)
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

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                bool updated =
                    categoryService.Update(Category);

                if (!updated)
                {
                    ErrorMessage =
                        "No se pudo actualizar la categoría.";

                    return Page();
                }

                return RedirectToPage("/Categories/Index");
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Ocurrió un error al actualizar la categoría.";

                return Page();
            }
        }
    }
}