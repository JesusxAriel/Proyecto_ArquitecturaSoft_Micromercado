namespace PuntoMarket.Usuarios.Application.Ports.Autenticacion;

public interface IUsuarioAutenticacionRepositorio
{
    Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default);
}
