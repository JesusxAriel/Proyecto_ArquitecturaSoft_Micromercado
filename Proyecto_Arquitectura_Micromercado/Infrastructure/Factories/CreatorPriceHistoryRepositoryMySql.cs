using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorPriceHistoryRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo)
        : CreatorPriceHistoryRepository
    {
        protected override IPriceHistoryRepository CrearRepositorio()
        {
            return new PriceHistoryRepositoryConErroresTraducidos(
                new MySqlPriceHistoryRepository(unidadDeTrabajo));
        }
    }
}
