using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto para Proveedor.
    public class CreatorSupplierRepository(DatabaseConnection conexion) : CreatorRepositorio<ISupplierRepository>
    {
        protected override ISupplierRepository CrearRepositorio()
        {
            return new MySqlSupplierRepository(conexion);
        }
    }
}
