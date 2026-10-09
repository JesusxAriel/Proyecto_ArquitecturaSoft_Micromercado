using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto: elige la implementación MySQL del repositorio de Categoría.
    public sealed class CreatorCategoryRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo)
        : CreatorCategoryRepository
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new MySqlCategoryRepository(unidadDeTrabajo);
        }
    }
}
