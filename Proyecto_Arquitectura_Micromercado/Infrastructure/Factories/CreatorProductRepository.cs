using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorProductRepository : CreatorRepositorio<IProductRepository>
    {
        private readonly IConfiguration _configuration;
        private readonly IPriceHistoryRepository _priceHistoryRepository;

        public CreatorProductRepository(IConfiguration configuration, IPriceHistoryRepository priceHistoryRepository)
        {
            _configuration = configuration;
            _priceHistoryRepository = priceHistoryRepository;
        }

        public override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository(_configuration, _priceHistoryRepository);
        }
    }
}
