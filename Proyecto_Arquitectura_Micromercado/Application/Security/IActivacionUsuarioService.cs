namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IActivacionUsuarioService
{
    Task<bool> TokenValidoAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<bool> ActivarAsync(
        string token,
        string nuevaContrasena,
        CancellationToken cancellationToken = default);
}
