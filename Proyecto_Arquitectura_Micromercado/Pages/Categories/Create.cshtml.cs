using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class CreateModel : CategoryFormModel
    {
        [TempData]
        public string? StatusMessage { get; set; }

        public CreateModel(ICategoryService categoryService)
            : base(categoryService)
        {
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
                bool created = CategoryService.Create(Category);

                if (!created)
                {
                    ErrorMessage = "No se pudo crear la categoría.";
                    return Page();
                }

                StatusMessage = "Categoría creada correctamente.";
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