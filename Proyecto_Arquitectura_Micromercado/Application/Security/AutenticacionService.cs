namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public sealed class AutenticacionService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher) : IAutenticacionService
{
    // Hash de relleno para verificar cuando el usuario no existe. Sin esto, responder a un
    // usuario inexistente seria mucho mas rapido que a uno real, y esa diferencia de
    // tiempo permitiria descubrir que nombres de usuario existen.
    private readonly Lazy<string> hashDeRelleno = new(() => hasher.Hash("relleno"));

    public async Task<UsuarioAutenticado?> AutenticarAsync(
        string nombreUsuario,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrEmpty(contrasena))
        {
            return null;
        }

        var credenciales = await usuarios.ObtenerCredencialesAsync(
            nombreUsuario.Trim(),
            cancellationToken);

        if (credenciales is null)
        {
            hasher.Verify(contrasena, hashDeRelleno.Value);
            return null;
        }

        if (!hasher.Verify(contrasena, credenciales.HashContrasena) || !credenciales.EstaActivo)
        {
            return null;
        }

        return new UsuarioAutenticado(
            credenciales.Id,
            credenciales.NombreUsuario,
            credenciales.NombreCompleto,
            credenciales.Rol);
    }
}
