using Microsoft.Extensions.DependencyInjection;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.DarDeBajaUsuario;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarActivacion;

namespace PuntoMarket.Usuarios.Application.DependencyInjection;

public static class RegistroGestionDeUsuarios
{
    public static IServiceCollection AgregarGestionDeUsuarios(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddScoped<CrearUsuarioCasoDeUso>();
        servicios.AddScoped<ListarUsuariosCasoDeUso>();
        servicios.AddScoped<ActivarCuentaCasoDeUso>();
        servicios.AddScoped<DarDeBajaUsuarioCasoDeUso>();
        servicios.AddScoped<ReenviarActivacionCasoDeUso>();

        return servicios;
    }
}
