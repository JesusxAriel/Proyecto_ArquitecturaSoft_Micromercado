using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Proyecto_Arquitectura_Micromercado.Models;
using Proyecto_Arquitectura_Micromercado.Repositories;
using Proyecto_Arquitectura_Micromercado.Validaciones;

namespace Proyecto_Arquitectura_Micromercado.Pages
{
    public class CategoriaEditarModel : PageModel
    {
        private readonly ICategoriaRepository categoriaRepository;
        private readonly ValidacionCategoria validacionCategoria;

        [BindProperty]
        public Categoria Categoria { get; set; } = new Categoria();

        public string MensajeError { get; set; } = string.Empty;

        public CategoriaEditarModel(ICategoriaRepository categoriaRepository)
        {
            this.categoriaRepository = categoriaRepository;
            validacionCategoria = new ValidacionCategoria();
        }

        public IActionResult OnGet(int id)
        {
            Categoria? categoriaEncontrada =
                categoriaRepository.ObtenerPorId(id);

            if (categoriaEncontrada == null)
            {
                return NotFound();
            }

            Categoria = categoriaEncontrada;

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!validacionCategoria.EsCategoriaValida(Categoria))
            {
                MensajeError = "Los datos ingresados no son válidos.";
                return Page();
            }

            try
            {
                Categoria.IdUsuarioAdmin = 1;

                bool actualizado =
                    categoriaRepository.Actualizar(Categoria);

                if (!actualizado)
                {
                    MensajeError =
                        "No se pudo actualizar la categoría.";

                    return Page();
                }

                return RedirectToPage("Categorias");
            }
            catch (Exception)
            {
                MensajeError =
                    "Ocurrió un error al actualizar la categoría.";

                return Page();
            }
        }
    }
}