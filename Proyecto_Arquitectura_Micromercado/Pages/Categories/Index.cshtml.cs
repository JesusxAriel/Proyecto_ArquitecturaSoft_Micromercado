using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryService categoryService;

        public PagedResult<Category> PagedCategories { get; private set; } =
            new PagedResult<Category>();

        public IReadOnlyList<Category> Categories => PagedCategories.Items;

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int Tamano { get; set; } = PageSizes.Default;

        [BindProperty(SupportsGet = true)]
        public string? Q { get; set; }

        private object ListRouteValues => new { Pagina, Tamano, Q };

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

        public bool ShowEditModal { get; private set; }

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
                PagedCategories =
                    await categoryService.GetPagedAsync(
                        Pagina,
                        Tamano,
                        Q,
                        cancellationToken);

                Pagina = PagedCategories.Page;
                Tamano = PagedCategories.PageSize;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "Error al cargar las categorías: " +
                    ex.Message;
            }
        }

        // Endpoints GET para el script de validación en vivo (site.js).
        public async Task<IActionResult> OnGetCheckUniqueAsync(
            string? nombre,
            int id,
            CancellationToken cancellationToken)
        {
            bool duplicate =
                !string.IsNullOrWhiteSpace(nombre) &&
                await categoryService.IsNameTakenAsync(
                    nombre,
                    id,
                    cancellationToken);

            return UniqueCheckResult.ToJson(
                duplicate,
                CategoryValidation.NameDuplicateMessage);
        }

        public async Task<IActionResult> OnGetPreviewCodeAsync(
            string? name,
            CancellationToken cancellationToken)
        {
            CategoryCodePreview preview =
                await categoryService.PreviewCodeAsync(
                    name ?? string.Empty,
                    cancellationToken);

            return new JsonResult(new
            {
                code = preview.Code,
                message = preview.Message
            });
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

                return RedirectToPage(ListRouteValues);
            }
            catch (ArgumentException ex)
            {
                ShowCreateModal = true;

                // Un nombre repetido (carrera con otra solicitud) se muestra junto al campo Nombre.
                ModelState.AddModelError(
                    ex is DuplicateCategoryNameException
                        ? "CreateCategory.Name"
                        : string.Empty,
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

            return RedirectToPage(ListRouteValues);
        }

        public async Task<IActionResult> OnPostEditAsync(
            CancellationToken cancellationToken)
        {
            ModelState.Clear();

            if (!TryValidateModel(
                    EditCategory,
                    nameof(EditCategory)))
            {
                ShowEditModal = true;

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

                return RedirectToPage(ListRouteValues);
            }
            catch (ArgumentException ex)
            {
                ShowEditModal = true;

                ModelState.AddModelError(
                    ex is DuplicateCategoryNameException
                        ? "EditCategory.Name"
                        : string.Empty,
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