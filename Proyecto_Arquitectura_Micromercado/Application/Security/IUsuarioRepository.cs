namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IUsuarioRepository
{
    Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string correo,
        CancellationToken cancellationToken = default);

    Task<CredencialesDeUsuario?> ObtenerCredencialesPorIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task ActualizarPasswordAsync(
        int usuarioId,
        string nuevoPasswordHash,
        CancellationToken cancellationToken = default);

    Task<bool> EstaActivoAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}