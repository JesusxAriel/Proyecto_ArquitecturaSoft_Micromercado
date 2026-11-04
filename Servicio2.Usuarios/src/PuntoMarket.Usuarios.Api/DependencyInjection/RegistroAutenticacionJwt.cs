using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PuntoMarket.Usuarios.Infrastructure.DependencyInjection;
using PuntoMarket.Usuarios.Infrastructure.Security;

namespace PuntoMarket.Usuarios.Api.DependencyInjection;

public static class RegistroAutenticacionJwt
{
    public static IServiceCollection AgregarAutenticacionJwt(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var opciones = configuracion
            .GetSection(JwtEmisorTokenOpciones.SectionName)
            .Get<JwtEmisorTokenOpciones>() ?? new JwtEmisorTokenOpciones();
        opciones.Validar();

        servicios.AgregarInfraestructuraAutenticacion(configuracion);
        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcionesJwt =>
            {
                opcionesJwt.MapInboundClaims = false;
                opcionesJwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(opciones.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Issuer,
                    ValidateAudience = true,
                    ValidAudience = opciones.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = "role",
                    NameClaimType = "unique_name"
                };
            });

        return servicios;
    }
}
