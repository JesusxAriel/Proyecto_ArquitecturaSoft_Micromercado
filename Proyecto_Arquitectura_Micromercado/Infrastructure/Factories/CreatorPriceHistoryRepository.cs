using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorPriceHistoryRepository : CreatorRepositorio<IPriceHistoryRepository>
    {
        public override IPriceHistoryRepository CrearRepositorio()
        {
            return new MySqlPriceHistoryRepository();
        }
    }
}
