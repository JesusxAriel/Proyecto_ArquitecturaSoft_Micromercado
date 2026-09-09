using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Models;
using Proyecto_Arquitectura_Micromercado.Repositories;

namespace Proyecto_Arquitectura_Micromercado.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ICategoriaRepository categoriaRepository;

        public string MensajeError { get; set; } = string.Empty;

        public List<Categoria> ListCategorias { get; set; } =
            new List<Categoria>();

        public IndexModel(ICategoriaRepository categoriaRepository)
        {
            this.categoriaRepository = categoriaRepository;
        }

        public void OnGet()
        {
            CargarCategorias();
        }

        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                int idUsuarioAdmin = 1;

                bool eliminado =
                    categoriaRepository.Desactivar(
                        id,
                        idUsuarioAdmin);

                if (!eliminado)
                {
                    MensajeError =
                        "No se pudo eliminar la categoría.";

                    CargarCategorias();
                    return Page();
                }

                return RedirectToPage("/Categories/Index");
            }
            catch (Exception)
            {
                MensajeError =
                    "Ocurrió un error al eliminar la categoría.";

                CargarCategorias();
                return Page();
            }
        }

        private void CargarCategorias()
        {
            try
            {
                ListCategorias =
                    categoriaRepository.ObtenerActivas();
            }
            catch (Exception ex)
            {
                MensajeError =
                    "Error al cargar categorías: "
                    + ex.Message;
            }
        }
    }
}