namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IAutenticacionService
{
    Task<UsuarioAutenticado?> AutenticarAsync(
        string nombreUsuario,
        string contrasena,
        CancellationToken cancellationToken = default);
}
