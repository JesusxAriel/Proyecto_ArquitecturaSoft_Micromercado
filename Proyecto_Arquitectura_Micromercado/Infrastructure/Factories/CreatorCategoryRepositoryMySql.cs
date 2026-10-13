using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorCategoryRepositoryMySql(
        MySqlUnidadDeTrabajo unidadDeTrabajo,
        IUsuarioActual usuarioActual)
        : CreatorCategoryRepository
    {
        protected override ICategoryRepository CrearRepositorio()
        {
            return new CategoryRepositoryConErroresTraducidos(
                new MySqlCategoryRepository(unidadDeTrabajo, usuarioActual));
        }
    }
}
