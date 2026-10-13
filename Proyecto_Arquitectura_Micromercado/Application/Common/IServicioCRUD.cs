namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public interface IServicioCRUD<TEntidad, TId>
{
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
