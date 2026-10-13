using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface ICategoryService :
    IServicioCRUD<Category, int>,
    IConListado,
    IConListadoPaginado
{
    Task<bool> IsNameTakenAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default);

    Task<CategoryCodePreview> PreviewCodeAsync(
        string name,
        CancellationToken cancellationToken = default);
}