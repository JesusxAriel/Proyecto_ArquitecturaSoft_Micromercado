using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorCategoryRepositoryEnMemoria(IUsuarioActual? usuarioActual = null)
        : CreatorCategoryRepository
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new InMemoryCategoryRepository(usuarioActual);
        }
    }
}
