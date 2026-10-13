using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorSupplierRepositoryEnMemoria : CreatorSupplierRepository
    {
        protected override ISupplierRepository CrearRepositorio()
        {
            return new InMemorySupplierRepository();
        }
    }
}
