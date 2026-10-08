using Proyecto_Arquitectura_Micromercado.Application.Products;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Raíz de la jerarquía de Creadores del repositorio de Producto.
    // Ver CreatorCategoryRepository para la justificación de esta capa.
    public abstract class CreatorProductRepository : CreatorRepositorio<IProductRepository>
    {
    }
}
