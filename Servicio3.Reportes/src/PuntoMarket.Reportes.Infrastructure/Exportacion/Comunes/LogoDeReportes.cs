using System.Reflection;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class LogoDeReportes
{
    private const string NombreDelRecurso = "PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes.logo.jpg";

    public static byte[] Obtener()
    {
        using var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(NombreDelRecurso)
            ?? throw new InvalidOperationException("No se encontró el logo incrustado de los reportes.");
        using var memoria = new MemoryStream();
        flujo.CopyTo(memoria);
        return memoria.ToArray();
    }
}
