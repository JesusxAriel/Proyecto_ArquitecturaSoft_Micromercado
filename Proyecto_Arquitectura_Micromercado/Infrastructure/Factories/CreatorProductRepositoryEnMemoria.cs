using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorProductRepositoryEnMemoria(
        CreatorPriceHistoryRepository creatorHistorialPrecios,
        IUsuarioActual? usuarioActual = null) : CreatorProductRepository
    {
        protected override IProductRepository CrearRepositorio()
        {
            return new InMemoryProductRepository(
                creatorHistorialPrecios.ObtenerRepositorio(),
                usuarioActual: usuarioActual);
        }
    }
}
