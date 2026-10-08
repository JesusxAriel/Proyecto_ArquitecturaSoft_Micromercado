using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Segundo Creador Concreto de la jerarquía de Proveedor.
    public sealed class CreatorSupplierRepositoryEnMemoria : CreatorSupplierRepository
    {
        protected override ISupplierRepository CrearRepositorio()
        {
            return new InMemorySupplierRepository();
        }
    }
}
