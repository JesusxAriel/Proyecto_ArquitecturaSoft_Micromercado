namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public interface IUsuarioRepository
{
    Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default);
}
