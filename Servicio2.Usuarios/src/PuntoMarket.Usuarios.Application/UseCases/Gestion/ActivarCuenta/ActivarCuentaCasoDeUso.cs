using System.Text.RegularExpressions;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;

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
        if (string.IsNullOrWhiteSpace(entrada.NuevaContrasena))
            throw new ArgumentException("La contraseña es obligatoria.");
        if (!string.Equals(entrada.NuevaContrasena, entrada.ConfirmarContrasena, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas no coinciden.");
        if (entrada.NuevaContrasena.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        if (!Regex.IsMatch(entrada.NuevaContrasena, @"[A-Z]"))
            throw new ArgumentException("La contraseña debe contener al menos una letra mayúscula.");
        if (!Regex.IsMatch(entrada.NuevaContrasena, @"[a-z]"))
            throw new ArgumentException("La contraseña debe contener al menos una letra minúscula.");
        if (!Regex.IsMatch(entrada.NuevaContrasena, @"[0-9]"))
            throw new ArgumentException("La contraseña debe contener al menos un número.");
        if (!Regex.IsMatch(entrada.NuevaContrasena, @"[\W_]"))
            throw new ArgumentException("La contraseña debe contener al menos un carácter especial.");

        var tokenHash = generadorToken.CalcularHash(entrada.Token);
        if (!await repositorio.TokenValidoAsync(tokenHash, reloj.UtcNow.UtcDateTime, cancellationToken))
            throw new InvalidOperationException("El token de activación no es válido o ha expirado.");

        var passwordHash = hasheador.Hash(entrada.NuevaContrasena);
        if (!await repositorio.ActivarAsync(tokenHash, passwordHash, reloj.UtcNow.UtcDateTime, cancellationToken))
            throw new InvalidOperationException("El token de activación no es válido o ha expirado.");

        return new ActivarCuentaSalida(true);
    }
}
