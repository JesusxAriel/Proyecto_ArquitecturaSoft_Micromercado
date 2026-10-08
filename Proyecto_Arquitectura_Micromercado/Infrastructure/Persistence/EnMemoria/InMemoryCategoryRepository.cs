using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Producto Concreto alternativo de ICategoryRepository: guarda las categorías en memoria.
// Respeta el mismo contrato que MySqlCategoryRepository (baja lógica, unicidad de nombre
// entre activas, unicidad de código sobre todas las filas, y las mismas excepciones),
// de modo que un servicio no pueda distinguir con cuál de los dos está trabajando.
public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private const int SystemAdminId = 1;

    private readonly List<Category> categorias = [];
    private int ultimoId;

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Category> resultado = Activas()
            .OrderBy(c => TextoEnMemoria.Normalizar(c.Name), StringComparer.Ordinal)
            .ThenBy(c => c.Id)
            .Select(Clonar)
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var coincidencias = Activas()
            .Where(c => string.IsNullOrWhiteSpace(search)
                || TextoEnMemoria.Contiene(c.Name, search)
                || TextoEnMemoria.Contiene(c.Code, search)
                || TextoEnMemoria.Contiene(c.AisleLocation, search)
                || TextoEnMemoria.Contiene(c.Description, search))
            .OrderBy(c => TextoEnMemoria.Normalizar(c.Name), StringComparer.Ordinal)
            .ThenBy(c => c.Id)
            .Select(Clonar);

        return Task.FromResult(PaginadorEnMemoria.Paginar(coincidencias, page, pageSize));
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var encontrada = Activas().FirstOrDefault(c => c.Id == id);

        return Task.FromResult(encontrada is null ? null : Clonar(encontrada));
    }

    public Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ValidarUnicidad(category, idExcluido: 0);

        var nueva = Clonar(category);
        nueva.Id = ++ultimoId;
        nueva.IsActive = true;
        nueva.AdminUserId = SystemAdminId;
        nueva.CreatedAt = DateTime.Now;
        categorias.Add(nueva);

        category.Id = nueva.Id;

        return Task.FromResult(nueva.Id);
    }

    public Task<bool> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        var existente = Activas().FirstOrDefault(c => c.Id == category.Id);

        if (existente is null)
        {
            return Task.FromResult(false);
        }

        ValidarUnicidad(category, idExcluido: category.Id);

        existente.Name = category.Name;
        existente.Description = category.Description;
        existente.Code = category.Code;
        existente.AisleLocation = category.AisleLocation;
        existente.AdminUserId = SystemAdminId;
        existente.UpdatedAt = DateTime.Now;

        return Task.FromResult(true);
    }

    public Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existente = Activas().FirstOrDefault(c => c.Id == id);

        if (existente is null)
        {
            return Task.FromResult(false);
        }

        existente.IsActive = false;
        existente.AdminUserId = SystemAdminId;

        return Task.FromResult(true);
    }

    public Task<bool> ExistsNameAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        var existe = Activas().Any(c =>
            c.Id != idExcluido && TextoEnMemoria.SonIguales(c.Name, name));

        return Task.FromResult(existe);
    }

    // Igual que en MySQL: devuelve todos los códigos, incluidas las categorías dadas de baja,
    // porque el índice UNIQUE del código también las considera.
    public Task<IReadOnlyList<string>> GetAllCodesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> codigos = categorias.Select(c => c.Code).ToList();

        return Task.FromResult(codigos);
    }

    private IEnumerable<Category> Activas() => categorias.Where(c => c.IsActive);

    // Reproduce los dos índices UNIQUE de la tabla CATEGORIAS.
    private void ValidarUnicidad(Category category, int idExcluido)
    {
        if (Activas().Any(c => c.Id != idExcluido && TextoEnMemoria.SonIguales(c.Name, category.Name)))
        {
            throw new DuplicateCategoryNameException();
        }

        if (categorias.Any(c => c.Id != idExcluido && TextoEnMemoria.SonIguales(c.Code, category.Code)))
        {
            throw new DuplicateCategoryCodeException();
        }
    }

    // Se clona al entrar y al salir para que el llamador no pueda mutar el almacén por
    // referencia, igual que una base de datos devuelve copias y no punteros a sus filas.
    private static Category Clonar(Category origen) => new()
    {
        Id = origen.Id,
        Name = origen.Name,
        Description = origen.Description,
        Code = origen.Code,
        AisleLocation = origen.AisleLocation,
        IsActive = origen.IsActive,
        AdminUserId = origen.AdminUserId,
        CreatedAt = origen.CreatedAt,
        UpdatedAt = origen.UpdatedAt
    };
}
