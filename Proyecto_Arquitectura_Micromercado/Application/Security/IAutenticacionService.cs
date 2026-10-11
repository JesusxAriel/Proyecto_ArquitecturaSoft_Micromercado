namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Puerto de entrada del caso de uso "iniciar sesion".
public interface IAutenticacionService
{
    // Devuelve el usuario si el nombre y la contrasena son correctos y el usuario esta
    // activo; null en cualquier otro caso. No distingue la causa a proposito: la pagina
    // muestra un mensaje generico para no revelar si el usuario existe.
    Task<UsuarioAutenticado?> AutenticarAsync(
        string nombreUsuario,
        string contrasena,
        CancellationToken cancellationToken = default);
}
