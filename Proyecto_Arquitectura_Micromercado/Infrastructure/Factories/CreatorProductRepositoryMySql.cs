using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Creador Concreto: elige la implementación MySQL del repositorio de Producto.
    //
    // Es el único Creador que necesita otro producto para construir el suyo: el
    // repositorio de historial de precios. Depende del Creador ABSTRACTO de historial,
    // no del concreto MySQL, así que no conoce el motor de esa dependencia.
    //
    // El historial se obtiene con ObtenerRepositorio(), nunca con un new directo ni con
    // CrearRepositorio(): así se reutiliza la misma instancia que el contenedor DI
    // entrega al resto de la petición, en vez de crear una paralela.
    public sealed class CreatorProductRepositoryMySql(
        DatabaseConnection conexion,
        CreatorPriceHistoryRepository creatorHistorialPrecios) : CreatorProductRepository
    {
        protected override IProductRepository CrearRepositorio()
        {
            return new MySqlProductRepository(
                conexion,
                creatorHistorialPrecios.ObtenerRepositorio());
        }
    }
}
