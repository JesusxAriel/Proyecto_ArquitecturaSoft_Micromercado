using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

internal sealed class CategoryRepositoryConErroresTraducidos(ICategoryRepository interno)
    : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetAllAsync(cancellationToken));

    public Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPagedAsync(page, pageSize, search, cancellationToken));

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetByIdAsync(id, cancellationToken));

    public Task<int> CreateAsync(Category entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.CreateAsync(entity, cancellationToken));

    public Task<bool> UpdateAsync(Category entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.UpdateAsync(entity, cancellationToken));

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.SoftDeleteAsync(id, cancellationToken));

    public Task<bool> ExistsNameAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.ExistsNameAsync(name, idExcluido, cancellationToken));

    public Task<IReadOnlyList<string>> GetAllCodesAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetAllCodesAsync(cancellationToken));
}

internal sealed class SupplierRepositoryConErroresTraducidos(ISupplierRepository interno)
    : ISupplierRepository
{
    public Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetAllAsync(cancellationToken));

    public Task<PagedResult<SupplierListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPagedAsync(page, pageSize, search, cancellationToken));

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetByIdAsync(id, cancellationToken));

    public Task<int> CreateAsync(Supplier entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.CreateAsync(entity, cancellationToken));

    public Task<bool> UpdateAsync(Supplier entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.UpdateAsync(entity, cancellationToken));

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.SoftDeleteAsync(id, cancellationToken));

    public Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.ExistsNombreEmpresaAsync(nombreEmpresa, idExcluido, cancellationToken));
}

internal sealed class ProductRepositoryConErroresTraducidos(IProductRepository interno)
    : IProductRepository
{
    public Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetAllAsync(cancellationToken));

    public Task<PagedResult<ProductListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPagedAsync(page, pageSize, search, cancellationToken));

    public Task<PagedResult<ProductListItem>> GetPagedForSalesAsync(
        int page, int pageSize, string? search, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPagedForSalesAsync(page, pageSize, search, cancellationToken));

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetByIdAsync(id, cancellationToken));

    public Task<int> CreateAsync(Product entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.CreateAsync(entity, cancellationToken));

    public Task<bool> UpdateAsync(Product entity, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.UpdateAsync(entity, cancellationToken));

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.SoftDeleteAsync(id, cancellationToken));

    public Task<bool> ExistsNombreEmpaqueAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.ExistsNombreEmpaqueAsync(nombre, idEmpaque, idExcluido, cancellationToken));

    public Task<bool> ExistenProductosActivosDelProveedorAsync(
        int idProveedor,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.ExistenProductosActivosDelProveedorAsync(idProveedor, cancellationToken));

    public Task<ResultadoBusquedaParaVenta> BuscarParaVentaAsync(
        string termino,
        int limite,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.BuscarParaVentaAsync(termino, limite, cancellationToken));

    public Task<decimal?> ObtenerPrecioVigenteAsync(
        int idProducto,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.ObtenerPrecioVigenteAsync(idProducto, cancellationToken));

    public Task<DescripcionDeProducto?> ObtenerDescripcionAsync(
        int idProducto,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.ObtenerDescripcionAsync(idProducto, cancellationToken));

    public Task<IReadOnlyList<LookupOption>> GetPackagingsAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPackagingsAsync(cancellationToken));

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetCategoriesAsync(cancellationToken));

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetSuppliersAsync(cancellationToken));
}

internal sealed class PriceHistoryRepositoryConErroresTraducidos(IPriceHistoryRepository interno)
    : IPriceHistoryRepository
{
    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetPriceHistoryAsync(cancellationToken));

    public Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.GetPriceHistoryByProductIdAsync(productId, cancellationToken));

    public Task<ProductPriceHistory?> GetPriceHistoryByIdAsync(
        int historyId,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.GetPriceHistoryByIdAsync(historyId, cancellationToken));

    public Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.AddPriceHistoryAsync(history, cancellationToken));
}

internal sealed class LoteRepositoryConErroresTraducidos(ILoteRepository interno)
    : ILoteRepository
{
    public Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(
        int idProducto,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.ObtenerVigentesParaDescuentoAsync(idProducto, cancellationToken));

    public Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.DescontarAsync(idLote, cantidad, cancellationToken));

    public Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.RestaurarAsync(idLote, cantidad, cancellationToken));

    public Task<IReadOnlyList<LoteDisponible>> ObtenerVencidosConStockAsync(
        DateOnly hoy,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.ObtenerVencidosConStockAsync(hoy, cancellationToken));

    public Task DesactivarAsync(int idLote, int? idUsuario, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.DesactivarAsync(idLote, idUsuario, cancellationToken));
}

internal sealed class VentaRepositoryConErroresTraducidos(IVentaRepository interno)
    : IVentaRepository
{
    public Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetByIdAsync(idVenta, cancellationToken));

    public Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetAllAsync(cancellationToken));

    public Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.GetVentasDelDiaAsync(cancellationToken));

    public Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.InsertarCabeceraAsync(idCliente, idUsuario, cancellationToken));

    public Task InsertarDetalleAsync(
        int idVenta,
        int idProducto,
        int idLote,
        int cantidad,
        decimal precioUnitario,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.InsertarDetalleAsync(idVenta, idProducto, idLote, cantidad, precioUnitario, cancellationToken));

    public Task CerrarVentaAsync(
        int idVenta,
        string nroComprobante,
        decimal montoTotal,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.CerrarVentaAsync(idVenta, nroComprobante, montoTotal, cancellationToken));

    public Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.ObtenerParaAnularAsync(idVenta, cancellationToken));

    public Task MarcarAnuladaAsync(
        int idVenta,
        string motivo,
        int idUsuarioAnulacion,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(
            () => interno.MarcarAnuladaAsync(idVenta, motivo, idUsuarioAnulacion, cancellationToken));
}
