using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

public sealed class InMemoryUsuarioRepository : IUsuarioRepository
{
    private readonly List<CredencialesDeUsuario> usuarios = [];

    public InMemoryUsuarioRepository Agregar(CredencialesDeUsuario usuario)
    {
        usuarios.Add(usuario);
        return this;
    }

    public Task<CredencialesDeUsuario?> ObtenerCredencialesAsync(
        string correo,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(usuarios.FirstOrDefault(usuario =>
            string.Equals(usuario.Correo, correo.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<CredencialesDeUsuario?> ObtenerCredencialesPorIdAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(usuarios.FirstOrDefault(usuario => usuario.Id == usuarioId));
    }

    public Task ActualizarPasswordAsync(
        int usuarioId,
        string nuevoPasswordHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var indice = usuarios.FindIndex(usuario => usuario.Id == usuarioId);
        if (indice >= 0)
        {
            usuarios[indice] = usuarios[indice] with
            {
                HashContrasena = nuevoPasswordHash,
                DebeCambiarPassword = false
            };
        }

        return Task.CompletedTask;
    }

    public Task<bool> EstaActivoAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var usuario = usuarios.FirstOrDefault(usuario => usuario.Id == usuarioId);
        return Task.FromResult(usuario is { EstaActivo: true, Estado: "ACTIVO" });
    }
}
