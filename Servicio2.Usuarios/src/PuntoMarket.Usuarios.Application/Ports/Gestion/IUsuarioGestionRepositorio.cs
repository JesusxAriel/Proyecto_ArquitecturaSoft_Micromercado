namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IUsuarioGestionRepositorio
{
    Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancellationToken = default);

    Task<bool> EsRolActivoAsync(int idRol, CancellationToken cancellationToken = default);

    Task<int> CrearAsync(UsuarioAltaInterna usuario, CancellationToken cancellationToken = default);

    Task<UsuarioActivacionDto?> ObtenerParaActivacionAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<string?> RenovarTokenActivacionAsync(
        int usuarioId,
        string tokenHash,
        DateTime tokenExpiraUtc,
        CancellationToken cancellationToken = default);

    Task<bool> DesactivarAsync(
        int usuarioId,
        int idUsuarioEliminacion,
        CancellationToken cancellationToken = default);

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
