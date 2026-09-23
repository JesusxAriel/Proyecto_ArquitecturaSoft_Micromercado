using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorProductRepository : CreatorRepositorio<IProductRepository>
    {
        public override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository();
        }
    }
}
