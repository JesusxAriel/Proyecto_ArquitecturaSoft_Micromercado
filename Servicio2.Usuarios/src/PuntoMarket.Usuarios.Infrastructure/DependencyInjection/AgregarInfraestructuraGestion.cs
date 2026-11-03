using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Infrastructure.Email;
using PuntoMarket.Usuarios.Infrastructure.Persistence;
using PuntoMarket.Usuarios.Infrastructure.Security;
using PuntoMarket.Usuarios.Infrastructure.Time;

namespace PuntoMarket.Usuarios.Infrastructure.DependencyInjection;

public static class RegistroInfraestructuraGestion
{
    public static IServiceCollection AgregarInfraestructuraGestion(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios.TryAddSingleton<ConexionMySql>(_ =>
        {
            var cadenaConexion = configuracion.GetConnectionString("MySqlConnection");
            if (string.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new InvalidOperationException(
                    "Configure ConnectionStrings:MySqlConnection para la persistencia de usuarios.");
            }

            return new ConexionMySql(cadenaConexion);
        });
        servicios.TryAddSingleton<IHasheadorPassword, BCryptHasheadorPassword>();
        servicios.TryAddSingleton<IReloj, RelojSistema>();

        servicios.AddScoped<IUsuarioGestionRepositorio, MySqlUsuarioGestionRepositorio>();
        servicios.AddSingleton<IGeneradorTokenActivacion, GeneradorTokenActivacion>();
        servicios.AddSingleton<SmtpEnviadorCorreoOpciones>(_ =>
        {
            var opciones = new SmtpEnviadorCorreoOpciones();
            configuracion.GetSection("Smtp").Bind(opciones);
            opciones.BaseUrl = configuracion["Activacion:BaseUrl"] ?? string.Empty;
            return opciones;
        });
        servicios.AddTransient<IEnviadorCorreo, SmtpEnviadorCorreo>();

        return servicios;
    }
}
