using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Enums;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarActivacion;

public sealed class ReenviarActivacionCasoDeUso(
    IUsuarioGestionRepositorio repositorio,
    IUsuarioActual usuarioActual,
    IReloj reloj,
    IGeneradorTokenActivacion generadorToken,
    IEnviadorCorreo enviadorCorreo)
{
    public async Task<ReenviarActivacionSalida> EjecutarAsync(
        ReenviarActivacionEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        var idUsuarioActualizacion = usuarioActual.Id;
        if (usuarioActual.Rol != "Administrador" || idUsuarioActualizacion <= 0)
            throw new UnauthorizedAccessException("La administración de usuarios requiere el rol Administrador.");

        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        if (entrada.UsuarioId <= 0)
            throw new ArgumentException("El ID del usuario debe ser mayor a cero.");

        var usuario = await repositorio.ObtenerParaActivacionAsync(entrada.UsuarioId, cancellationToken);
        if (usuario is null)
            throw new InvalidOperationException("El usuario no existe.");
        if (usuario.Estado != EstadoUsuario.PENDIENTE || usuario.FechaActivacion is not null)
            throw new InvalidOperationException("Solo se puede reenviar la activación de una cuenta pendiente y no activada.");

        var token = generadorToken.Generar(reloj.UtcNow, TimeSpan.FromMinutes(10));
        var correo = await repositorio.RenovarTokenActivacionAsync(
            entrada.UsuarioId, token.Hash, token.Expira.UtcDateTime, idUsuarioActualizacion, cancellationToken);
        if (string.IsNullOrWhiteSpace(correo))
            throw new InvalidOperationException("No se pudo renovar el token de activación del usuario.");

        await enviadorCorreo.EnviarActivacionAsync(correo, token.Valor, token.Expira, cancellationToken);

        return new ReenviarActivacionSalida(true);
    }
}
