using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Producto Concreto alternativo de ISupplierRepository.
// Mismo contrato que MySqlSupplierRepository: baja lógica, unicidad del nombre de
// empresa entre proveedores activos y DuplicateSupplierException ante el duplicado.
public sealed class InMemorySupplierRepository : ISupplierRepository
{
    private readonly List<Supplier> proveedores = [];
    private int ultimoId;

    public Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SupplierListItem> resultado = Activos()
            .OrderBy(p => TextoEnMemoria.Normalizar(p.NombreEmpresa), StringComparer.Ordinal)
            .ThenBy(p => p.Id)
            .Select(AListItem)
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task<PagedResult<SupplierListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var coincidencias = Activos()
            .Where(p => string.IsNullOrWhiteSpace(search)
                || TextoEnMemoria.Contiene(p.NombreEmpresa, search)
                || TextoEnMemoria.Contiene(p.NumeroEmpresa, search)
                || TextoEnMemoria.Contiene(p.CorreoReferencia, search))
            .OrderBy(p => TextoEnMemoria.Normalizar(p.NombreEmpresa), StringComparer.Ordinal)
            .ThenBy(p => p.Id)
            .Select(AListItem);

        return Task.FromResult(PaginadorEnMemoria.Paginar(coincidencias, page, pageSize));
    }

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var encontrado = Activos().FirstOrDefault(p => p.Id == id);

        return Task.FromResult(encontrado is null ? null : Clonar(encontrado));
    }

    public Task<int> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        ValidarUnicidad(supplier, idExcluido: 0);

        var nuevo = Clonar(supplier);
        nuevo.Id = ++ultimoId;
        nuevo.EstaActivo = true;
        proveedores.Add(nuevo);

        supplier.Id = nuevo.Id;

        return Task.FromResult(nuevo.Id);
    }

    public Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        var existente = Activos().FirstOrDefault(p => p.Id == supplier.Id);

        if (existente is null)
        {
            return Task.FromResult(false);
        }

        ValidarUnicidad(supplier, idExcluido: supplier.Id);

        existente.NombreEmpresa = supplier.NombreEmpresa;
        existente.NumeroEmpresa = supplier.NumeroEmpresa;
        existente.CorreoReferencia = supplier.CorreoReferencia;
        existente.EsAutogestionado = supplier.EsAutogestionado;

        return Task.FromResult(true);
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

    public Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        var existe = Activos().Any(p =>
            p.Id != idExcluido && TextoEnMemoria.SonIguales(p.NombreEmpresa, nombreEmpresa));

        return Task.FromResult(existe);
    }

    private IEnumerable<Supplier> Activos() => proveedores.Where(p => p.EstaActivo);

    private void ValidarUnicidad(Supplier supplier, int idExcluido)
    {
        if (Activos().Any(p =>
            p.Id != idExcluido && TextoEnMemoria.SonIguales(p.NombreEmpresa, supplier.NombreEmpresa)))
        {
            throw new DuplicateSupplierException();
        }
    }

    private static SupplierListItem AListItem(Supplier origen) => new()
    {
        Id = origen.Id,
        NombreEmpresa = origen.NombreEmpresa,
        NumeroEmpresa = origen.NumeroEmpresa,
        CorreoReferencia = origen.CorreoReferencia ?? string.Empty,
        EsAutogestionado = origen.EsAutogestionado
    };

    private static Supplier Clonar(Supplier origen) => new()
    {
        Id = origen.Id,
        NombreEmpresa = origen.NombreEmpresa,
        NumeroEmpresa = origen.NumeroEmpresa,
        CorreoReferencia = origen.CorreoReferencia,
        EsAutogestionado = origen.EsAutogestionado,
        EstaActivo = origen.EstaActivo
    };
}
