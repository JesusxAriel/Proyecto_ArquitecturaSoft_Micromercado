using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public interface IConListadoPaginado
{
    Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
