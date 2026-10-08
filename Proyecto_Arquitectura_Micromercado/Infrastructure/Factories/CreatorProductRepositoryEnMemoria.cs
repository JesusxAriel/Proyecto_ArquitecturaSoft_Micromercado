using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Segundo Creador Concreto de la jerarquía de Producto.
    //
    // Depende del Creador ABSTRACTO de historial, exactamente igual que su par MySQL, y
    // obtiene el repositorio con ObtenerRepositorio() para compartir la misma instancia.
    public sealed class CreatorProductRepositoryEnMemoria(
        CreatorPriceHistoryRepository creatorHistorialPrecios) : CreatorProductRepository
    {
        protected override IProductRepository CrearRepositorio()
        {
            return new InMemoryProductRepository(creatorHistorialPrecios.ObtenerRepositorio());
        }
    }
}
