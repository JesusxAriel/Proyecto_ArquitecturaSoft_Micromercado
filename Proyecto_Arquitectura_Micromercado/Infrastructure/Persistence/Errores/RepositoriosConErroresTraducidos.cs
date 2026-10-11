using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

// Decoradores que envuelven a un repositorio y traducen los errores de su motor a
// ErrorDePersistenciaException.
//
// POR QUE UN DECORADOR Y NO UN TRY/CATCH EN CADA REPOSITORIO
// Habria que repetir el mismo try/catch en 27 metodos y reindentar su cuerpo: mucho
// ruido y mucho riesgo de romper SQL que hoy funciona. El decorador deja los
// repositorios intactos y pone la traduccion en un solo lugar por contrato.
//
// Quien decide envolver es el Creador Concreto de MySQL, que es justamente el que sabe
// que su producto habla con MySQL. El adaptador en memoria no se envuelve porque no
// tiene errores de motor que traducir.

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

    public Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default) =>
        ErroresDePersistencia.TraducirAsync(() => interno.AddPriceHistoryAsync(history, cancellationToken));
}
