using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public interface IProductRepository :
    IRepositorioBase<Product, ProductListItem, int>,
    IConListadoPaginado,
    IConVerificacionDeNombreYEmpaque,
    IConConsultaPorProveedor,
    IConCatalogo,
    IConPrecioVigente,
    IConBusquedaParaVenta
{
    Task<PagedResult<ProductListItem>> GetPagedForSalesAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default);
}
