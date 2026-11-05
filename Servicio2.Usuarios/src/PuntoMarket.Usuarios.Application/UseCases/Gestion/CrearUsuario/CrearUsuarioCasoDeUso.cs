using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Exceptions;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;

public sealed class CrearUsuarioCasoDeUso(
    IUsuarioGestionRepositorio repositorio,
    IUsuarioActual usuarioActual,
    IHasheadorPassword hasheador,
    IReloj reloj,
    IGeneradorTokenActivacion generadorToken,
    IEnviadorCorreo enviadorCorreo)
{
    public async Task<CrearUsuarioSalida> EjecutarAsync(
        CrearUsuarioEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var idCreador = usuarioActual.Id;
        if (usuarioActual.Rol != "Administrador" || idCreador <= 0)
            throw new UnauthorizedAccessException("La administración de usuarios requiere el rol Administrador.");

        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        var nombre = NormalizarEspacios(entrada.Nombre);
        var apellido = NormalizarEspacios(entrada.Apellido);
        var correo = (entrada.Correo ?? "").Trim().ToLowerInvariant();
        if (nombre.Length == 0 || apellido.Length == 0)
            throw new ArgumentException("Nombre y apellido son obligatorios.");
        if (nombre.Length + apellido.Length + 1 > 150 ||
            nombre.Any(char.IsControl) || apellido.Any(char.IsControl))
            throw new ArgumentException("El nombre completo debe tener hasta 150 caracteres válidos.");
        if (correo.Length > 150 ||
            !Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Ingrese un correo válido de hasta 150 caracteres.");
        if (entrada.IdRol <= 0)
            throw new ArgumentException("Debe seleccionar un rol existente y activo.");
        if (await repositorio.ExisteCorreoAsync(correo, cancellationToken))
            throw new UsuarioDuplicadoException();
        if (!await repositorio.EsRolActivoAsync(entrada.IdRol, cancellationToken))
            throw new ArgumentException("Debe seleccionar un rol existente y activo.");

        var baseUsername = GeneradorNombreDeUsuario.GenerarBase(nombre, apellido);
        var passwordHash = hasheador.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        var token = generadorToken.Generar(reloj.UtcNow, TimeSpan.FromMinutes(10));

        for (long sufijo = 0; ; sufijo = checked(sufijo + 1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var numero = sufijo == 0 ? "" : sufijo.ToString(CultureInfo.InvariantCulture);
            var username = baseUsername[..Math.Min(baseUsername.Length, 50 - numero.Length)] + numero;
            if (await repositorio.ExisteUsernameAsync(username, cancellationToken))
                continue;

            int id;
            try
            {
                id = await repositorio.CrearAsync(new UsuarioAltaInterna(
                    nombre, apellido, correo, username, entrada.IdRol, passwordHash, idCreador,
                    token.Hash, token.Expira.UtcDateTime), cancellationToken);
            }
            catch (UsuarioDuplicadoException)
            {
                if (await repositorio.ExisteCorreoAsync(correo, cancellationToken) ||
                    !await repositorio.ExisteUsernameAsync(username, cancellationToken))
                    throw;

                continue;
            }

            await enviadorCorreo.EnviarActivacionAsync(correo, token.Valor, token.Expira, cancellationToken);
            return new CrearUsuarioSalida(id);
        }
    }

    private static string NormalizarEspacios(string? valor) =>
        string.Join(" ", (valor ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
