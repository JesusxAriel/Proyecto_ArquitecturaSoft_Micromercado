using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Domain.Enums;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Autenticacion.CambiarPassword;

public sealed class CambiarPasswordCasoDeUso(
    IUsuarioAutenticacionRepositorio repositorio,
    IHasheadorPassword hasheador,
    IUsuarioActual usuarioActual)
{
    public async Task<CambiarPasswordSalida> EjecutarAsync(
        CambiarPasswordEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(entrada.ContrasenaActual))
            throw new ArgumentException(
                "La contraseña actual es obligatoria.",
                nameof(entrada.ContrasenaActual));
        if (!PoliticaDePassword.CumpleRequisitos(entrada.NuevaContrasena))
            throw new ArgumentException(
                "La nueva contraseña no cumple con los requisitos de seguridad.",
                nameof(entrada.NuevaContrasena));

        var usuarioId = usuarioActual.Id;
        if (usuarioId <= 0)
            throw new UnauthorizedAccessException("No se pudo autenticar al usuario.");

        var usuario = await repositorio.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null ||
            usuario.Estado != EstadoUsuario.ACTIVO ||
            !usuario.RolActivo)
        {
            throw new UnauthorizedAccessException("La cuenta no está activa.");
        }

        if (!hasheador.Verify(entrada.ContrasenaActual, usuario.PasswordHash))
        {
            throw new ArgumentException(
                "La contraseña actual es incorrecta.",
                nameof(entrada.ContrasenaActual));
        }

        if (hasheador.Verify(entrada.NuevaContrasena, usuario.PasswordHash))
        {
            throw new ArgumentException(
                "La nueva contraseña no puede ser igual a la contraseña actual.",
                "NuevaContrasenaIgualActual");
        }

        var nuevoHash = hasheador.Hash(entrada.NuevaContrasena);
        if (!await repositorio.ActualizarPasswordAsync(
                usuarioId,
                usuarioId,
                usuario.PasswordHash,
                nuevoHash,
                cancellationToken))
        {
            throw new UnauthorizedAccessException("La cuenta no está activa.");
        }

        return new CambiarPasswordSalida(true);
    }
}
