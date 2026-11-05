using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace PuntoMarket.Ventas.Frontend.Security;

public sealed record IdentidadTokenUsuarios(int Id, string Usuario, string Rol, bool DebeCambiarPassword, DateTimeOffset VenceEn);
public sealed class TokenUsuariosInvalidoException() : Exception("El token de Usuarios no es válido o ha vencido.");
public sealed class ValidadorTokenUsuarios(Func<string> leerClave, TimeProvider reloj)
{
    public IdentidadTokenUsuarios Validar(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 16384) throw new TokenUsuariosInvalidoException();
            var clave = leerClave();
            if (Encoding.UTF8.GetByteCount(clave) < 32) throw new TokenUsuariosInvalidoException();
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false, MaximumTokenSizeInBytes = 16384 };
            var principal = handler.ValidateToken(token, new TokenValidationParameters {
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                RequireSignedTokens = true, ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateIssuer = true, ValidIssuer = "PuntoMarket.DemoLocal",
                ValidateAudience = true, ValidAudience = "PuntoMarket.Ventas.DemoLocal",
                ValidateLifetime = true, RequireExpirationTime = true, ClockSkew = TimeSpan.Zero,
                NameClaimType = "unique_name", RoleClaimType = "role",
                // Reloj inyectado; sin tolerancia ni aceptación de tokens sin nbf/exp.
                LifetimeValidator = (nbf, exp, _, _) => nbf.HasValue && exp.HasValue && nbf.Value <= reloj.GetUtcNow().UtcDateTime && exp.Value > reloj.GetUtcNow().UtcDateTime && nbf.Value < exp.Value,
                IncludeTokenOnFailedValidation = false
            }, out var validado);
            // No se extraen claims hasta superar firma, algoritmo, emisor, audiencia y vigencia.
            static string Unico(ClaimsPrincipal p, string nombre) {
                var claims = p.FindAll(nombre).ToArray();
                return claims.Length == 1 ? claims[0].Value : throw new TokenUsuariosInvalidoException();
            }
            if (!int.TryParse(Unico(principal,"sub"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0) throw new TokenUsuariosInvalidoException();
            var usuario = Unico(principal,"unique_name");
            var rol = Unico(principal,"role");
            if (string.IsNullOrWhiteSpace(usuario) || rol is not ("Administrador" or "Cajero") || !bool.TryParse(Unico(principal,"debeCambiarPassword"), out var cambiar)) throw new TokenUsuariosInvalidoException();
            return new(id, usuario, rol, cambiar, new DateTimeOffset(DateTime.SpecifyKind(validado.ValidTo,DateTimeKind.Utc)));
        }
        catch (TokenUsuariosInvalidoException) { throw; }
        // No propagar excepciones de la biblioteca, JWT ni rutas/configuración privada.
        catch (Exception e) when (e is SecurityTokenException or ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException) { throw new TokenUsuariosInvalidoException(); }
    }
}
