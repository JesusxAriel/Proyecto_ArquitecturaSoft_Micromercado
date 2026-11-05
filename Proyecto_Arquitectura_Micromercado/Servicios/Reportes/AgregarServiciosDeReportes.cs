namespace Proyecto_Arquitectura_Micromercado.Servicios.Reportes;

public static class AgregarServiciosDeReportes
{
    private const string ClaveDeUrlBase = "Servicios:Reportes:BaseUrl";
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(30);

    public static IServiceCollection AgregarClienteDeReportes(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        servicios.AddScoped<ITokenDeAcceso, TokenDeAccesoDelUsuario>();
        servicios.AddHttpClient<IReportesApiClient, ReportesApiClient>(cliente =>
        {
            cliente.BaseAddress = ObtenerUrlBase(configuracion);
            cliente.Timeout = TiempoMaximo;
        });

        return servicios;
    }

    private static Uri? ObtenerUrlBase(IConfiguration configuracion) =>
        Uri.TryCreate(configuracion[ClaveDeUrlBase], UriKind.Absolute, out var url) ? url : null;
}
