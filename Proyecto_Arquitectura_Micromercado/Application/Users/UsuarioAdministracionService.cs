using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Application.Users;

public sealed class UsuarioAdministracionService(
    IUsuarioAdministracionRepository repositorio,
    IUsuarioActual usuarioActual,
    IPasswordHasher hasher,
    TimeProvider timeProvider,
    IEmailActivacionUsuario emailActivacion) : IUsuarioAdministracionService
{
    public async Task<bool> ReenviarActivacionAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        ExigirAdministrador();
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId));

        var token = GenerarTokenActivacion();
        var expira = timeProvider.GetUtcNow().AddMinutes(10);
        var correo = await repositorio.RenovarTokenActivacionAsync(
            usuarioId, token.Hash, expira.UtcDateTime, cancellationToken);
        if (correo is null)
            return false;

        try
        {
            await emailActivacion.EnviarEnlaceAsync(correo, token.Valor, expira, cancellationToken);
        }
        catch (ErrorEnvioCorreoException ex)
        {
            throw new ErrorEnvioActivacionException(usuarioId, ex);
        }

        return true;
    }

    public async Task<bool> DesactivarAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        ExigirAdministrador();
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId));
        if (usuarioId == usuarioActual.Id)
            throw new NoPuedeDesactivarSuPropiaCuentaException();

        return await repositorio.DesactivarAsync(usuarioId, usuarioActual.Id, cancellationToken);
    }

    public Task<IReadOnlyList<UsuarioListadoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        ExigirAdministrador();
        return repositorio.ListarAsync(cancellationToken);
    }

    public Task<IReadOnlyList<RolActivoDto>> ListarRolesActivosAsync(CancellationToken cancellationToken = default)
    {
        ExigirAdministrador();
        return repositorio.ListarRolesActivosAsync(cancellationToken);
    }

    public async Task<UsuarioCreadoDto> CrearAsync(
        UsuarioCreacionDto usuario,
        CancellationToken cancellationToken = default)
    {
        ExigirAdministrador();
        ArgumentNullException.ThrowIfNull(usuario);
        cancellationToken.ThrowIfCancellationRequested();
        var nombre = NormalizarEspacios(usuario.Nombre);
        var apellido = NormalizarEspacios(usuario.Apellido);
        var correo = (usuario.Correo ?? "").Trim().ToLowerInvariant();
        if (nombre.Length == 0 || apellido.Length == 0)
            throw new ArgumentException("Nombre y apellido son obligatorios.");
        if (nombre.Length + apellido.Length + 1 > 150 ||
            nombre.Any(char.IsControl) || apellido.Any(char.IsControl))
            throw new ArgumentException("El nombre completo debe tener hasta 150 caracteres válidos.");
        if (correo.Length > 150 || !Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Ingrese un correo válido de hasta 150 caracteres.");
        if (usuario.IdRol <= 0 || !await repositorio.EsRolActivoAsync(usuario.IdRol, cancellationToken))
            throw new ArgumentException("Debe seleccionar un rol existente y activo.");
        if (await repositorio.ExisteCorreoAsync(correo, cancellationToken))
            throw new CorreoUsuarioDuplicadoException();

        var letrasNombre = Letras(nombre);
        var primerApellido = Letras(apellido.Split(' ')[0]);
        if (letrasNombre.Length == 0 || primerApellido.Length == 0)
            throw new ArgumentException("Nombre y primer apellido deben contener letras latinas para generar el username.");
        var baseUsername = letrasNombre[..1] + primerApellido;
        var hash = HashDeContrasenaDescartable();
        var tokenActivacion = GenerarTokenActivacion();
        var tokenActivacionExpira = timeProvider.GetUtcNow().AddMinutes(10);
        var idCreador = usuarioActual.Id;
        for (long sufijo = 0; ; sufijo = checked(sufijo + 1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var numero = sufijo == 0 ? "" : sufijo.ToString(CultureInfo.InvariantCulture);
            var username = baseUsername[..Math.Min(baseUsername.Length, 50 - numero.Length)] + numero;
            if (await repositorio.ExisteUsernameAsync(username, cancellationToken)) continue;
            try
            {
                var id = await repositorio.CrearAsync(new UsuarioAltaInterna(
                    nombre, apellido, correo, username, usuario.IdRol, hash, idCreador,
                    tokenActivacion.Hash, tokenActivacionExpira.UtcDateTime), cancellationToken);
                try
                {
                    await emailActivacion.EnviarEnlaceAsync(
                        correo, tokenActivacion.Valor, tokenActivacionExpira, cancellationToken);
                }
                catch (ErrorEnvioCorreoException ex)
                {
                    throw new ErrorEnvioActivacionException(id, ex);
                }

                return new UsuarioCreadoDto(id);
            }
            catch (UsernameUsuarioDuplicadoException)
            {
                continue;
            }
        }
    }

    private sealed record TokenActivacion(string Valor, string Hash);

    private static TokenActivacion GenerarTokenActivacion()
    {
        var valor = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
        return new TokenActivacion(valor, hash);
    }

    private string HashDeContrasenaDescartable() =>
        hasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

    private void ExigirAdministrador()
    {
        if (usuarioActual.Rol != Roles.Administrador || usuarioActual.Id <= 0)
            throw new UnauthorizedAccessException("La administración de usuarios requiere el rol Administrador.");
    }

    private static string NormalizarEspacios(string? valor) =>
        string.Join(" ", (valor ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Letras(string valor) => new(valor.Normalize(NormalizationForm.FormD)
        .Where(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
        .Select(char.ToLowerInvariant).ToArray());
}
