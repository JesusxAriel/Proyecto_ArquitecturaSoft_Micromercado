using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Infrastructure.Security;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

internal static class JwtTokenPruebaFactory
{
    internal const string Clave =
        "jwt-clave-exclusiva-de-pruebas-hs256-2026-no-productiva-64-caracteres";
    internal const string ClaveAlternativa =
        "otra-clave-exclusiva-de-pruebas-hs256-2026-no-productiva-64-caracteres";
    internal const string Emisor = "PuntoMarket.Usuarios";
    internal const string Audiencia = "PuntoMarket";

    internal static readonly DateTimeOffset Ahora =
        new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    internal static JwtEmisorToken CrearEmisor(
        int expireMinutes = 60,
        string issuer = Emisor,
        string audience = Audiencia,
        string key = Clave,
        IReloj? reloj = null) =>
        new(CrearOpciones(expireMinutes, issuer, audience, key), reloj ?? new RelojFijo());

    internal static JwtEmisorTokenOpciones CrearOpciones(
        int expireMinutes = 60,
        string issuer = Emisor,
        string audience = Audiencia,
        string key = Clave) =>
        new()
        {
            Issuer = issuer,
            Audience = audience,
            Key = key,
            ExpireMinutes = expireMinutes
        };

    internal static IReloj CrearReloj() => new RelojFijo();

    internal static string CrearTokenFirmado(
        IEnumerable<Claim>? claims = null,
        string issuer = Emisor,
        string audience = Audiencia,
        string key = Clave,
        DateTimeOffset? emitidoEn = null,
        TimeSpan? duracion = null,
        IEnumerable<string>? excluirClaims = null)
    {
        var claimsFinales = new List<Claim>
        {
            new("sub", "42"),
            new("unique_name", "usuario-prueba"),
            new("role", "Administrador")
        };

        foreach (var claim in claims ?? [])
        {
            if (claimsFinales.Any(predeterminado => predeterminado.Type == claim.Type))
                claimsFinales.RemoveAll(predeterminado => predeterminado.Type == claim.Type);
            claimsFinales.Add(claim);
        }

        if (excluirClaims is not null)
        {
            var tiposExcluidos = excluirClaims.ToHashSet(StringComparer.Ordinal);
            claimsFinales.RemoveAll(claim => tiposExcluidos.Contains(claim.Type));
        }

        var inicio = emitidoEn ?? DateTimeOffset.UtcNow;
        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claimsFinales,
            inicio.UtcDateTime,
            inicio.Add(duracion ?? TimeSpan.FromHours(1)).UtcDateTime,
            credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class RelojFijo : IReloj
    {
        public DateTimeOffset UtcNow => Ahora;
    }
}
