using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

public sealed class JwtTokenValidator : IValidadorJwt
{
    private static readonly string[] ClaimsRequeridos =
        ["sub", "unique_name", "nombre", "role", "debeCambiarPassword"];

    private readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };
    private readonly TokenValidationParameters _parametros;

    public JwtTokenValidator(IOptions<JwtValidacionOpciones> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        var configuracion = opciones.Value;
        configuracion.Validar();

        _parametros = new TokenValidationParameters
        {
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracion.Key)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = configuracion.Issuer,
            ValidateAudience = true,
            ValidAudience = configuracion.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    }

    public ClaimsPrincipal Validar(string token)
        => ValidarConExpiracion(token).Principal;

    public JwtValidado ValidarConExpiracion(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new SecurityTokenException("El token JWT está vacío.");

        var principal = _handler.ValidateToken(token, _parametros, out var validatedToken);
        ValidarClaims(principal);
        if (validatedToken is not JwtSecurityToken jwt)
            throw new SecurityTokenValidationException("El token JWT validado tiene un formato inesperado.");

        var expiraUtc = new DateTimeOffset(
            DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc));
        return new JwtValidado(principal, expiraUtc);
    }

    private static void ValidarClaims(ClaimsPrincipal principal)
    {
        var claims = ClaimsRequeridos.ToDictionary(
            tipo => tipo,
            tipo => principal.FindAll(tipo).ToArray(),
            StringComparer.Ordinal);

        if (claims.Any(entrada =>
                entrada.Value.Length != 1 ||
                string.IsNullOrWhiteSpace(entrada.Value[0].Value)))
        {
            throw new SecurityTokenValidationException(
                "El token JWT no contiene exactamente un valor para cada claim requerido.");
        }

        if (!int.TryParse(
                claims["sub"][0].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var usuarioId) ||
            usuarioId <= 0)
        {
            throw new SecurityTokenValidationException(
                "El claim 'sub' del token JWT no contiene un identificador válido.");
        }

        if (!bool.TryParse(claims["debeCambiarPassword"][0].Value, out _))
        {
            throw new SecurityTokenValidationException(
                "El claim 'debeCambiarPassword' del token JWT no contiene un valor booleano.");
        }
    }
}
