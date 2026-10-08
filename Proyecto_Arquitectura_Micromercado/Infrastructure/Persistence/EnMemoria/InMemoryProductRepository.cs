using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Producto Concreto alternativo de IProductRepository.
//
// Igual que MySqlProductRepository, recibe el repositorio de historial y le delega el
// registro del cambio de precio al actualizar: la responsabilidad del historial no vive
// aqui, solo se la invoca (SRP).
//
// Los catalogos (empaques, categorias, proveedores) se reciben por constructor porque en
// MySQL provienen de otras tablas; aca se inyectan ya resueltos.
public sealed class InMemoryProductRepository(
    IPriceHistoryRepository priceHistoryRepository,
    IEnumerable<LookupOption>? empaques = null,
    IEnumerable<LookupOption>? categorias = null,
    IEnumerable<LookupOption>? proveedores = null) : IProductRepository
{
    private const int SystemAdminId = 1;
    private const string MotivoPorDefecto = "Actualización de precio";

    private readonly List<Product> productos = [];
    private readonly List<LookupOption> catalogoEmpaques = [.. empaques ?? []];
    private readonly List<LookupOption> catalogoCategorias = [.. categorias ?? []];
    private readonly List<LookupOption> catalogoProveedores = [.. proveedores ?? []];
    private int ultimoId;

    public Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductListItem> resultado = Ordenados(Activos()).ToList();

        return Task.FromResult(resultado);
    }

    public Task<PagedResult<ProductListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var coincidencias = Ordenados(Activos())
            .Where(p => string.IsNullOrWhiteSpace(search)
                || TextoEnMemoria.Contiene(p.Nombre, search)
                || TextoEnMemoria.Contiene(p.EmpaquePresentacion, search)
                || TextoEnMemoria.Contiene(p.NombreCategoria, search)
                || TextoEnMemoria.Contiene(p.NombreProveedor, search));

        return Task.FromResult(PaginadorEnMemoria.Paginar(coincidencias, page, pageSize));
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var encontrado = Activos().FirstOrDefault(p => p.Id == id);

        return Task.FromResult(encontrado is null ? null : Clonar(encontrado));
    }

    public Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        ValidarUnicidad(product, idExcluido: 0);

        var nuevo = Clonar(product);
        nuevo.Id = ++ultimoId;
        nuevo.EstaActivo = true;
        productos.Add(nuevo);

        product.Id = nuevo.Id;

        return Task.FromResult(nuevo.Id);
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existente = Activos().FirstOrDefault(p => p.Id == product.Id);

        if (existente is null)
        {
            return false;
        }

        ValidarUnicidad(product, idExcluido: product.Id);

        var precioVentaAnterior = existente.PrecioVenta;
        var precioCostoAnterior = existente.PrecioCosto;

        existente.Nombre = product.Nombre;
        existente.IdEmpaque = product.IdEmpaque;
        existente.PrecioVenta = product.PrecioVenta;
        existente.PrecioCosto = product.PrecioCosto;
        existente.StockMinimo = product.StockMinimo;
        existente.IdCategoria = product.IdCategoria;
        existente.IdProveedor = product.IdProveedor;

        // Mismo criterio que en MySQL: solo se registra si algun precio cambio.
        if (precioVentaAnterior != product.PrecioVenta || precioCostoAnterior != product.PrecioCosto)
        {
            await priceHistoryRepository.AddPriceHistoryAsync(
                new ProductPriceHistory
                {
                    IdProducto = product.Id,
                    NombreProducto = product.Nombre,
                    PrecioVentaAnterior = precioVentaAnterior,
                    PrecioVentaNuevo = product.PrecioVenta,
                    PrecioCostoAnterior = precioCostoAnterior,
                    PrecioCostoNuevo = product.PrecioCosto,
                    MotivoCambio = string.IsNullOrWhiteSpace(product.MotivoCambio)
                        ? MotivoPorDefecto
                        : product.MotivoCambio.Trim(),
                    IdUsuario = SystemAdminId
                },
                cancellationToken);
        }

        return true;
    }

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existente = Activos().FirstOrDefault(p => p.Id == id);

        if (existente is null)
        {
            return Task.FromResult(false);
        }

        existente.EstaActivo = false;

        return Task.FromResult(true);
    }

    public Task<bool> ExistsNombreEmpaqueAsync(
        string nombre,
        int idEmpaque,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        var existe = Activos().Any(p =>
            p.Id != idExcluido
            && p.IdEmpaque == idEmpaque
            && TextoEnMemoria.SonIguales(p.Nombre, nombre));

        return Task.FromResult(existe);
    }

    public Task<IReadOnlyList<LookupOption>> GetPackagingsAsync(CancellationToken cancellationToken = default) =>
        Catalogo(catalogoEmpaques);

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Catalogo(catalogoCategorias);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        Catalogo(catalogoProveedores);

    private static Task<IReadOnlyList<LookupOption>> Catalogo(List<LookupOption> origen)
    {
        IReadOnlyList<LookupOption> resultado = origen
            .OrderBy(o => TextoEnMemoria.Normalizar(o.Nombre), StringComparer.Ordinal)
            .ToList();

        return Task.FromResult(resultado);
    }

    private IEnumerable<Product> Activos() => productos.Where(p => p.EstaActivo);

    // Reproduce el indice UNIQUE (nombre, idEmpaque) entre productos activos.
    private void ValidarUnicidad(Product product, int idExcluido)
    {
        if (Activos().Any(p =>
            p.Id != idExcluido
            && p.IdEmpaque == product.IdEmpaque
            && TextoEnMemoria.SonIguales(p.Nombre, product.Nombre)))
        {
            throw new DuplicateProductException();
        }
    }

    private IEnumerable<ProductListItem> Ordenados(IEnumerable<Product> origen) => origen
        .OrderBy(p => TextoEnMemoria.Normalizar(p.Nombre), StringComparer.Ordinal)
        .ThenBy(p => p.Id)
        .Select(AListItem);

    private ProductListItem AListItem(Product origen) => new()
    {
        Id = origen.Id,
        Nombre = origen.Nombre,
        IdEmpaque = origen.IdEmpaque,
        EmpaquePresentacion = NombreDeCatalogo(catalogoEmpaques, origen.IdEmpaque),
        PrecioVenta = origen.PrecioVenta,
        PrecioCosto = origen.PrecioCosto,
        StockMinimo = origen.StockMinimo,
        IdCategoria = origen.IdCategoria,
        NombreCategoria = NombreDeCatalogo(catalogoCategorias, origen.IdCategoria),
        IdProveedor = origen.IdProveedor,
        NombreProveedor = NombreDeCatalogo(catalogoProveedores, origen.IdProveedor),

        // En MySQL lo calcula la vista vw_productos_con_stock a partir de los movimientos
        // de inventario, que este motor no modela.
        StockCalculado = 0
    };

    private static string NombreDeCatalogo(List<LookupOption> catalogo, int id) =>
        catalogo.FirstOrDefault(o => o.Id == id)?.Nombre ?? string.Empty;

    private static Product Clonar(Product origen) => new()
    {
        Id = origen.Id,
        Nombre = origen.Nombre,
        IdEmpaque = origen.IdEmpaque,
        PrecioVenta = origen.PrecioVenta,
        PrecioCosto = origen.PrecioCosto,
        StockMinimo = origen.StockMinimo,
        IdCategoria = origen.IdCategoria,
        IdProveedor = origen.IdProveedor,
        MotivoCambio = origen.MotivoCambio,
        EstaActivo = origen.EstaActivo
    };
}
