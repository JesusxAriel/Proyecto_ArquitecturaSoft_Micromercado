using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Proyecto_Arquitectura_Micromercado.Servicios;

public static class RegistroUsuariosApi
{
    private const string ClaveBaseUrl = "UsuariosApi:BaseUrl";

    public static IServiceCollection AddUsuariosApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration[ClaveBaseUrl];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(baseAddress.Host) ||
            !string.IsNullOrEmpty(baseAddress.UserInfo) ||
            !string.IsNullOrEmpty(baseAddress.Query) ||
            !string.IsNullOrEmpty(baseAddress.Fragment))
        {
            throw new InvalidOperationException(
                $"La configuración '{ClaveBaseUrl}' es obligatoria y debe ser una URL absoluta HTTP o HTTPS, sin credenciales, consulta ni fragmento.");
        }

        if (!baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
            baseAddress = new Uri(baseAddress.AbsoluteUri + "/", UriKind.Absolute);

        services.AddHttpClient<UsuariosApiClient>(client => client.BaseAddress = baseAddress);
        return services;
    }
}
