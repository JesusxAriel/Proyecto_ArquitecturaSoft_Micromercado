using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Segundo Creador Concreto de la jerarquía de Historial de Precios.
    public sealed class CreatorPriceHistoryRepositoryEnMemoria : CreatorPriceHistoryRepository
    {
        protected override IPriceHistoryRepository CrearRepositorio()
        {
            return new InMemoryPriceHistoryRepository();
        }
    }
}
