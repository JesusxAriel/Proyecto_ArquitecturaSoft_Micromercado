using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

public static class RegistroValidacionJwt
{
    public static IServiceCollection AddJwtTokenValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<JwtValidacionOpciones>, ValidarJwtValidacionOpciones>();
        services.AddOptions<JwtValidacionOpciones>()
            .Bind(configuration.GetSection(JwtValidacionOpciones.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidadorJwt, JwtTokenValidator>();

        return services;
    }
}
