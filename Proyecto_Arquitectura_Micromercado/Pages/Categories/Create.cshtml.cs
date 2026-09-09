using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly ICategoryService categoryService;

        [BindProperty]
        public Category Category { get; set; } = new Category();

        public string ErrorMessage { get; set; } = string.Empty;

        public CreateModel(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                bool created =
                    categoryService.Create(Category);

                if (!created)
                {
                    ErrorMessage =
                        "No se pudo crear la categoría.";

                    return Page();
                }

                return RedirectToPage("/Categories/Index");
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Ocurrió un error al crear la categoría.";

                return Page();
            }
        }
    }
}