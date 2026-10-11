using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Adaptador en memoria del puerto IUsuarioRepository, para probar el login sin base de
// datos. La busqueda ignora mayusculas, igual que la collation de la tabla USUARIO.
public sealed class InMemoryUsuarioRepository : IUsuarioRepository
{
    private readonly List<CredencialesDeUsuario> usuarios = [];

    public InMemoryUsuarioRepository Agregar(CredencialesDeUsuario usuario)
    {
        usuarios.Add(usuario);
        return this;
    }

    public Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string nombreUsuario,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(usuarios.FirstOrDefault(usuario =>
            string.Equals(usuario.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase)));
}
