using System.Security.Cryptography;
using System.Text;

namespace Proyecto_Arquitectura_Micromercado.Application.Security;

public sealed class ActivacionUsuarioService(
    IActivacionUsuarioRepository usuarios,
    IPasswordHasher hasher,
    TimeProvider timeProvider) : IActivacionUsuarioService
{
    public Task<bool> TokenValidoAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(false);
        }

        return usuarios.TokenValidoAsync(
            CalcularHash(token),
            timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }

    public async Task<bool> ActivarAsync(
        string token,
        string nuevaContrasena,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)
            || !PasswordSeguraAttribute.CumpleRequisitos(nuevaContrasena))
        {
            return false;
        }

        var tokenHash = CalcularHash(token);
        var ahoraUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!await usuarios.TokenValidoAsync(tokenHash, ahoraUtc, cancellationToken))
        {
            return false;
        }

        return await usuarios.ActivarAsync(
            tokenHash,
            hasher.Hash(nuevaContrasena),
            ahoraUtc,
            cancellationToken);
    }

    private static string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
