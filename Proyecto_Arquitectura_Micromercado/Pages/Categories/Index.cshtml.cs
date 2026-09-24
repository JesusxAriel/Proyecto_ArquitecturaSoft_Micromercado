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

        public Category CreateCategory { get; set; } =
            new Category();

        [BindProperty]
        public Category EditCategory { get; set; } =
            new Category();

        [BindProperty]
        public int DeleteCategoryId { get; set; }

        public bool ShowCreateModal { get; private set; }

        public IndexModel(ICategoryService categoryService)
        {
            this.categoryService = categoryService;
        }

        public async Task OnGetAsync(
            CancellationToken cancellationToken)
        {
            await LoadCategoriesAsync(cancellationToken);
        }

        private async Task LoadCategoriesAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<Category> categories =
                    await categoryService.GetAllAsync(
                        cancellationToken);

                Categories = categories
                    .OrderBy(category => category.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Error al cargar las categorías: " +
                    ex.Message;
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(
            [FromForm(Name = "CreateCategory")]
            Category createCategory,
            CancellationToken cancellationToken)
        {
            CreateCategory = createCategory;
            ModelState.Clear();

            if (!TryValidateModel(
                    CreateCategory,
                    nameof(CreateCategory)))
            {
                ShowCreateModal = true;

                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }

            try
            {
                await categoryService.CreateAsync(
                    CreateCategory,
                    cancellationToken);

                StatusMessage =
                    "Categoría creada correctamente.";

                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ShowCreateModal = true;

                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }
            catch (Exception)
            {
                ShowCreateModal = true;

                ErrorMessage =
                    "Ocurrió un error al crear la categoría.";

                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(
            CancellationToken cancellationToken)
        {
            if (DeleteCategoryId <= 0)
            {
                return BadRequest(
                    "La categoría no es válida.");
            }

            bool deleted =
                await categoryService.SoftDeleteAsync(
                    DeleteCategoryId,
                    cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            StatusMessage =
                "Categoría eliminada correctamente.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            CancellationToken cancellationToken)
        {
            ModelState.Clear();

            if (!TryValidateModel(
                    EditCategory,
                    nameof(EditCategory)))
            {
                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }

            try
            {
                bool updated =
                    await categoryService.UpdateAsync(
                        EditCategory,
                        cancellationToken);

                if (!updated)
                {
                    ErrorMessage =
                        "No se pudo actualizar la categoría.";

                    await LoadCategoriesAsync(
                        cancellationToken);

                    return Page();
                }

                StatusMessage =
                    "Categoría actualizada correctamente.";

                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    ex.Message);

                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Ocurrió un error al actualizar la categoría.";

                await LoadCategoriesAsync(
                    cancellationToken);

                return Page();
            }
        }
    }
}