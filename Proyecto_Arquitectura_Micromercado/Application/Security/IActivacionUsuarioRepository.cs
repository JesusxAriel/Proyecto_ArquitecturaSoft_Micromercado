namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IActivacionUsuarioRepository
{
    Task<bool> TokenValidoAsync(
        string tokenHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default);

    Task<bool> ActivarAsync(
        string tokenHash,
        string passwordHash,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default);
}
