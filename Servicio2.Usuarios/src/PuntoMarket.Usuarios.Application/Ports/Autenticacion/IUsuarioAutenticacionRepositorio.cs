namespace PuntoMarket.Usuarios.Application.Ports.Autenticacion;

public interface IUsuarioAutenticacionRepositorio
{
    Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default);

    Task<UsuarioAutenticacionDto?> ObtenerPorIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<bool> ActualizarPasswordAsync(
        int usuarioId,
        string passwordHashActual,
        string passwordHash,
        CancellationToken cancellationToken = default);
}
