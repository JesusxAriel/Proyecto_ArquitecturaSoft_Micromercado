namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Puerto de salida para consultar usuarios al iniciar sesion.
public interface IUsuarioRepository
{
    // Busca por nombre de usuario sin distinguir mayusculas. Devuelve null si no existe
    // o si su rol esta inactivo.
    Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default);
}
