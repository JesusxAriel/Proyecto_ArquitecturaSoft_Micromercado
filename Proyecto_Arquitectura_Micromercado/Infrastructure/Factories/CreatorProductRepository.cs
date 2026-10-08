using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorProductRepository : CreatorRepositorio<IProductRepository>
    {
        private readonly CreatorRepositorio<IPriceHistoryRepository> priceHistoryCreator;

        // El repositorio de historial se obtiene de su propia fábrica mediante ObtenerRepositorio(),
        // nunca con new directo ni con CrearRepositorio(): así se reutiliza la misma instancia
        // que el contenedor DI entrega al resto de la petición, en vez de crear una paralela.
        public CreatorProductRepository(CreatorRepositorio<IPriceHistoryRepository> priceHistoryCreator)
        {
            this.priceHistoryCreator = priceHistoryCreator;
        }

        protected override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository(priceHistoryCreator.ObtenerRepositorio());
        }
    }
}
