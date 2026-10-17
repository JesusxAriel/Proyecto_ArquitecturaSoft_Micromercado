using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductService :
    IServicioCRUD<Product, int>,
    IConListado,
    IConListadoPaginado,
    IConCatalogo,
    IConHistorialPrecios
{
    Task<PagedResult<ProductListItem>> GetPagedForSalesAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default);

    Task<ResultadoBusquedaParaVenta> BuscarParaVentaAsync(
        string? termino,
        CancellationToken cancellationToken = default);

    Task<bool> IsDuplicateAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default);
}
