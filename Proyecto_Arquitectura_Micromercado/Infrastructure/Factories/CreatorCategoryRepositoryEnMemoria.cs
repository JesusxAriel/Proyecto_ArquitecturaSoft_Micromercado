using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Segundo Creador Concreto de la jerarquía de Categoría: elige la implementación en
    // memoria. Es el que demuestra que el punto de extensión del patrón es real: el
    // cliente sigue pidiendo un CreatorCategoryRepository y no se entera del cambio.
    public sealed class CreatorCategoryRepositoryEnMemoria : CreatorCategoryRepository
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new InMemoryCategoryRepository();
        }
    }
}
