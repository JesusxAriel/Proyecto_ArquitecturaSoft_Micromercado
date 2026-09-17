namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public interface IServicioCRUD<TEntidad, TId, TCrearDto, TActualizarDto>
{
    Task<TEntidad?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task<TId> CreateAsync(
        TCrearDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        TActualizarDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
