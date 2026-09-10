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

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public Category EditCategory { get; set; } = new Category();

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
                Categories = categoryService.GetActive()
                    .OrderBy(category => category.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Error al cargar las categorías: " + ex.Message;
            }
        }

        public IActionResult OnPostEdit()
        {
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return Page();
            }

            try
            {
                if (!categoryService.Update(EditCategory))
                {
                    ErrorMessage = "No se pudo actualizar la categoría.";
                    LoadCategories();
                    return Page();
                }

                StatusMessage = "Categoría actualizada correctamente.";
                return RedirectToPage();
            }
            catch (Exception)
            {
                ErrorMessage = "Ocurrió un error al actualizar la categoría.";
                LoadCategories();
                return Page();
            }
        }
    }
}