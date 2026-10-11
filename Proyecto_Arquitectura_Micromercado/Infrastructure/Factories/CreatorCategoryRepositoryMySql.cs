using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto: elige la implementación MySQL del repositorio de Categoría.
    public sealed class CreatorCategoryRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo)
        : CreatorCategoryRepository
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new CategoryRepositoryConErroresTraducidos(
                new MySqlCategoryRepository(unidadDeTrabajo));
        }
    }
}
