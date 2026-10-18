namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public sealed class AutenticacionService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher) : IAutenticacionService
{
    private readonly Lazy<string> hashDeRelleno = new(() => hasher.Hash("relleno"));

    public async Task<UsuarioAutenticado?> AutenticarAsync(
        string correo,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(contrasena))
        {
            return null;
        }

        var credenciales = await usuarios.ObtenerCredencialesAsync(
            correo.Trim(),
            cancellationToken);

        if (credenciales is null)
        {
            hasher.Verify(contrasena, hashDeRelleno.Value);
            return null;
        }

        // Validar contraseña y que esté activo
        if (!hasher.Verify(contrasena, credenciales.HashContrasena) || !credenciales.EstaActivo)
        {
            return null;
        }

        // REQUISITO DE LA PARTE 2: Bloquear si está PENDIENTE o INACTIVO
        if (credenciales.Estado != "ACTIVO")
        {
            return null;
        }

        return new UsuarioAutenticado(
            credenciales.Id,
            credenciales.NombreUsuario,
            credenciales.NombreCompleto,
            credenciales.Rol,
            credenciales.DebeCambiarPassword); // Por si ocupas pasar este flag al token/sesión
    }
}