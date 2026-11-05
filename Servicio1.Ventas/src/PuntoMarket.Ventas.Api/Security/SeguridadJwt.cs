using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Ventas.Api.Errors;

namespace PuntoMarket.Ventas.Api.Security;

public static class SeguridadJwt
{
    public const string OperarVentas = "OperarVentas";
    public const string AnularVentas = "AnularVentas";

    public static IServiceCollection AgregarSeguridadJwt(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Jwt:Key"];
        var issuer = config["Jwt:Issuer"];
        var audience = config["Jwt:Audience"];
        var habilitado = !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(issuer) && !string.IsNullOrWhiteSpace(audience);
        var alguno = new[] { key, issuer, audience }.Any(v => !string.IsNullOrWhiteSpace(v));
        if (alguno && (!habilitado || Encoding.UTF8.GetByteCount(key!) < 32))
            throw new InvalidOperationException("Configurar Jwt:Key (mínimo 32 bytes UTF-8), Jwt:Issuer y Jwt:Audience conjuntamente.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = habilitado ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)) : null,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "unique_name",
                RoleClaimType = "role"
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Sin configuración no se aceptan tokens; salud sigue disponible.
                    if (!habilitado) context.Fail("Autenticación no configurada.");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    if (!IdentidadValida(context.Principal!)) context.Fail("Claims de identidad inválidos.");
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.Headers.WWWAuthenticate = "Bearer";
                    await ErroresHttp.EscribirAsync(context.HttpContext, 401, "Se requiere un token válido.");
                },
                OnForbidden = context => ErroresHttp.EscribirAsync(context.HttpContext, 403, "No tiene permiso para esta operación.")
            };
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy(OperarVentas, policy => policy.RequireAuthenticatedUser()
                .RequireRole("Administrador", "Cajero").RequireAssertion(PasswordActualizada));
            options.AddPolicy(AnularVentas, policy => policy.RequireAuthenticatedUser()
                .RequireRole("Administrador").RequireAssertion(PasswordActualizada));
        });
        return services;
    }

    private static bool IdentidadValida(ClaimsPrincipal usuario)
    {
        string? Unico(string tipo)
        {
            var claims = usuario.FindAll(tipo).ToArray();
            return claims.Length == 1 ? claims[0].Value : null;
        }
        return int.TryParse(Unico("sub"), NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            && !string.IsNullOrWhiteSpace(Unico("unique_name"))
            && !string.IsNullOrWhiteSpace(Unico("role"))
            && bool.TryParse(Unico("debeCambiarPassword"), out _);
    }

    private static bool PasswordActualizada(AuthorizationHandlerContext context) =>
        bool.TryParse(context.User.FindFirst("debeCambiarPassword")?.Value, out var obligatorio) && !obligatorio;
}
