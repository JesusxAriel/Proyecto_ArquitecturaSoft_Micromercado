using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorProductRepository : CreatorRepositorio<IProductRepository>
    {
        private readonly CreatorRepositorio<IPriceHistoryRepository> priceHistoryCreator;

        // El repositorio de historial también se obtiene de su propia fábrica, nunca con new directo.
        public CreatorProductRepository(CreatorRepositorio<IPriceHistoryRepository> priceHistoryCreator)
        {
            this.priceHistoryCreator = priceHistoryCreator;
        }

        public override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository(priceHistoryCreator.CrearRepositorio());
        }
    }
}
