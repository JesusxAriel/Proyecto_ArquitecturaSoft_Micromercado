using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto: elige la implementación MySQL del repositorio de Proveedor.
    public sealed class CreatorSupplierRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo)
        : CreatorSupplierRepository
    {
        protected override ISupplierRepository CrearRepositorio()
        {
            return new MySqlSupplierRepository(unidadDeTrabajo);
        }
    }
}
