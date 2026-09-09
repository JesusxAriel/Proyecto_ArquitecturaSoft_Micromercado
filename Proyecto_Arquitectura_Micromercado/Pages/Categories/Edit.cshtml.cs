using Microsoft.AspNetCore.Mvc;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class EditModel : CategoryFormModel
    {
        public EditModel(ICategoryService categoryService)
            : base(categoryService)
        {
        }

        public IActionResult OnGet(int id)
        {
            Category? category =
                CategoryService.GetById(id);

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
                    CategoryService.Update(Category);

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