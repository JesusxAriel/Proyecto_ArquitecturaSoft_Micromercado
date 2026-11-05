using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.CambiarPassword;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.IniciarSesion;

namespace PuntoMarket.Usuarios.Application.DependencyInjection;

public static class RegistroAutenticacionDeUsuarios
{
    public static IServiceCollection AgregarAutenticacionDeUsuarios(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<IniciarSesionCasoDeUso>();
        servicios.AddScoped<CambiarPasswordCasoDeUso>();

        return servicios;
    }
}
