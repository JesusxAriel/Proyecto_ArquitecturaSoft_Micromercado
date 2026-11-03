using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;

public sealed class ActivarCuentaCasoDeUso(
    IUsuarioGestionRepositorio repositorio,
    IHasheadorPassword hasheador,
    IReloj reloj,
    IGeneradorTokenActivacion generadorToken)
{
    public async Task<ActivarCuentaSalida> EjecutarAsync(
        ActivarCuentaEntrada entrada,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(entrada.Token))
            throw new ArgumentException("El token de activación es obligatorio.");

        if (!PoliticaDePassword.CumpleRequisitos(entrada.NuevaContrasena))
            throw new ArgumentException("La contraseña no cumple con los requisitos de seguridad.");

        if (!string.Equals(entrada.NuevaContrasena, entrada.ConfirmarContrasena, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas no coinciden.");

        var tokenHash = generadorToken.CalcularHash(entrada.Token);

        if (!await repositorio.TokenValidoAsync(
            tokenHash, reloj.UtcNow.UtcDateTime, cancellationToken))
            throw new InvalidOperationException("El token de activación no es válido o ha expirado.");

        var passwordHash = hasheador.Hash(entrada.NuevaContrasena);

        if (!await repositorio.ActivarAsync(
            tokenHash, passwordHash, reloj.UtcNow.UtcDateTime, cancellationToken))
            throw new InvalidOperationException("El token de activación no es válido o ha expirado.");

        return new ActivarCuentaSalida(true);
    }
}