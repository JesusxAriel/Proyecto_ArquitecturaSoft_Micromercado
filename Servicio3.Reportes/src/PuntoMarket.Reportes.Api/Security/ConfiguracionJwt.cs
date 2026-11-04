using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PuntoMarket.Reportes.Api.Security;

public static class ConfiguracionJwt
{
    private const string ClaimDeNombre = "unique_name";
    private const string ClaimDeRol = "role";

    public static IServiceCollection AgregarAutenticacionJwt(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var parametrosDeToken = CrearParametrosDeValidacion(configuracion);

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones =>
            {
                opciones.MapInboundClaims = false;
                opciones.TokenValidationParameters = parametrosDeToken;
            });
        servicios.AddAuthorization();

        return servicios;
    }

    private static TokenValidationParameters CrearParametrosDeValidacion(IConfiguration configuracion)
    {
        var clave = configuracion["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(clave))
            throw new InvalidOperationException("Falta la configuración 'Jwt:Key' (variable de entorno Jwt__Key).");

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuracion["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuracion["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimDeNombre,
            RoleClaimType = ClaimDeRol
        };
    }
}
