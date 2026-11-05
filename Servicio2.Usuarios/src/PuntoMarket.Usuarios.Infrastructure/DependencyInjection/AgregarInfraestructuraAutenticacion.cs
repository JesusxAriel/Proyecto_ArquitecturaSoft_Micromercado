using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Infrastructure.Security;
using PuntoMarket.Usuarios.Infrastructure.Time;

namespace PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

public static class RegistroInfraestructuraAutenticacion
{
    public static IServiceCollection AgregarInfraestructuraAutenticacion(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var opciones = configuracion
            .GetSection(JwtEmisorTokenOpciones.SectionName)
            .Get<JwtEmisorTokenOpciones>() ?? new JwtEmisorTokenOpciones();
        opciones.Validar();

        servicios.TryAddSingleton<IReloj, RelojSistema>();
        servicios.TryAddSingleton<IEmisorToken>(
            proveedor => new JwtEmisorToken(opciones, proveedor.GetRequiredService<IReloj>()));

        return servicios;
    }
}
