using Proyecto_Arquitectura_Micromercado.Application.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Raíz de la jerarquía de Creadores del repositorio de Proveedor.
    // Ver CreatorCategoryRepository para la justificación de esta capa.
    public abstract class CreatorSupplierRepository : CreatorRepositorio<ISupplierRepository>
    {
    }
}
