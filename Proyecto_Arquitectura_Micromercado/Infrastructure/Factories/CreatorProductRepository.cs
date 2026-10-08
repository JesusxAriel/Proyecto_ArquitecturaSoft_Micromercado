using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto para Producto. Es el único que necesita otro producto para
    // construir el suyo: el repositorio de historial de precios.
    //
    // Ese repositorio se obtiene de su propia fábrica mediante ObtenerRepositorio(),
    // nunca con un new directo ni con CrearRepositorio(): así se reutiliza la misma
    // instancia que el contenedor DI entrega al resto de la petición, en vez de crear
    // una paralela.
    public class CreatorProductRepository(
        DatabaseConnection conexion,
        CreatorPriceHistoryRepository creatorHistorialPrecios) : CreatorRepositorio<IProductRepository>
    {
        protected override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository(
                conexion,
                creatorHistorialPrecios.ObtenerRepositorio());
        }
    }
}
