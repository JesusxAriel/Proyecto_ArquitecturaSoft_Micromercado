namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public interface IRepositorioBase<TEntidad, TListItem, TId>
{
    Task<IReadOnlyList<TListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<TEntidad?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task<TId> CreateAsync(
        TEntidad entity,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        TEntidad entity,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
