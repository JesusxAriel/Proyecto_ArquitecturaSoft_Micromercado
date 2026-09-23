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

        public Category CreateCategory { get; set; } = new Category();

        [BindProperty]
        public Category EditCategory { get; set; } = new Category();

        [BindProperty]
        public int DeleteCategoryId { get; set; }

        public bool ShowCreateModal { get; private set; }

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

        public async Task<IActionResult> OnPostCreateAsync(
            [FromForm(Name = "CreateCategory")] Category createCategory,
            CancellationToken cancellationToken)
        {
            CreateCategory = createCategory;

            // EditCategory sigue siendo [BindProperty] del mismo tipo Category. Cuando
            // el formulario de Crear postea, no llega ningún campo "EditCategory.*",
            // así que el model binder de ASP.NET Core cae al "prefijo vacío" para
            // EditCategory y genera errores de validación con claves sin prefijo
            // que contaminan el ModelState de Crear. Se limpia y se revalida solo
            // CreateCategory, con su prefijo correcto, sin tocar EditCategory.
            ModelState.Clear();

            if (!TryValidateModel(CreateCategory, nameof(CreateCategory)))
            {
                ShowCreateModal = true;
                LoadCategories();
                return Page();
            }

            try
            {
                await categoryService.CreateAsync(CreateCategory, cancellationToken);
                StatusMessage = "Categoría creada correctamente.";
                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ShowCreateModal = true;
                ModelState.AddModelError(string.Empty, ex.Message);
                LoadCategories();
                return Page();
            }
            catch (Exception)
            {
                ShowCreateModal = true;
                ErrorMessage = "Ocurrió un error al crear la categoría.";
                LoadCategories();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
        {
            if (DeleteCategoryId <= 0)
            {
                return BadRequest("La categoría no es válida.");
            }

            if (!await categoryService.SoftDeleteAsync(DeleteCategoryId, cancellationToken))
            {
                return NotFound();
            }

            StatusMessage = "Categoría eliminada correctamente.";
            return RedirectToPage();
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
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                LoadCategories();
                return Page();
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