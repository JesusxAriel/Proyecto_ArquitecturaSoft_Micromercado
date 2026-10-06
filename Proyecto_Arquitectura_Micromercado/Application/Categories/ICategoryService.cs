using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface ICategoryService :
    IServicioCRUD<Category, int, Category, Category>,
    IConListado,
    IConListadoPaginado
{
    // Misma comparación que al guardar: sin distinguir mayúsculas, tildes ni espacios extra.
    Task<bool> IsNameTakenAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default);

    // Usa la misma generación que CreateAsync, sin guardar nada.
    Task<CategoryCodePreview> PreviewCodeAsync(
        string name,
        CancellationToken cancellationToken = default);
}