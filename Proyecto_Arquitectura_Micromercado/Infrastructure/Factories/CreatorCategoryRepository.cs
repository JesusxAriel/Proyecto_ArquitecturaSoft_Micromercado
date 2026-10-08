using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto para Categoría: decide que el producto es la implementación MySQL
    // y le entrega la conexión que recibió por constructor.
    public class CreatorCategoryRepository(DatabaseConnection conexion) : CreatorRepositorio<ICategoryRepository>
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new MySqlCategoryRepository(conexion);
        }
    }
}
