using Proyecto_Arquitectura_Micromercado.Application.Categories;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    // Raíz de la jerarquía de Creadores del repositorio de Categoría.
    //
    // Existe para que el eje de variación del patrón sea el MOTOR DE PERSISTENCIA y no
    // la entidad. Es un tipo no genérico, así que sus subclases (MySQL, en memoria, ...)
    // comparten un tipo base concreto al que cualquier cliente puede apuntar sin saber
    // qué motor está detrás. Eso es lo que hace posible el polimorfismo del patrón:
    // CreatorRepositorio<ICategoryRepository> y CreatorRepositorio<IProductRepository>
    // son tipos sin relación entre sí, por lo que el genérico solo no alcanzaba.
    public abstract class CreatorCategoryRepository : CreatorRepositorio<ICategoryRepository>
    {
    }
}
