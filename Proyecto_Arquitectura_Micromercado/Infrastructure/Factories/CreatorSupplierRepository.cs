using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorSupplierRepository : CreatorRepositorio<ISupplierRepository>
    {
        public override ISupplierRepository CrearRepositorio()
        {
            return new MySqlSupplierRepository();
        }
    }
}
