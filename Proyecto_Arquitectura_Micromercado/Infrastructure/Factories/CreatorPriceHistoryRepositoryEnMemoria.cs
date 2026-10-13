using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorPriceHistoryRepositoryEnMemoria : CreatorPriceHistoryRepository
    {
        protected override IPriceHistoryRepository CrearRepositorio()
        {
            return new InMemoryPriceHistoryRepository();
        }
    }
}
