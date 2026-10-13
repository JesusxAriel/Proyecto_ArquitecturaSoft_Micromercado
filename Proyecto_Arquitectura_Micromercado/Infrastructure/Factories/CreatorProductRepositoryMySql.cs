using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class CreatorProductRepositoryMySql(
        MySqlUnidadDeTrabajo unidadDeTrabajo,
        CreatorPriceHistoryRepository creatorHistorialPrecios,
        IUsuarioActual usuarioActual) : CreatorProductRepository
    {
        protected override IProductRepository CrearRepositorio()
        {
            return new ProductRepositoryConErroresTraducidos(
                new MySqlProductRepository(
                    unidadDeTrabajo,
                    creatorHistorialPrecios.ObtenerRepositorio(),
                    usuarioActual));
        }
    }
}
