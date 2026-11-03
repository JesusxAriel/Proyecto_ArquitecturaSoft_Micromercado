using System.Reflection;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.VentasPorCategoria;

public static class LogoDelReporte
{
    private const string NombreDelRecurso = "PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes.logo.jpg";

    public static byte[] Obtener()
    {
        using var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(NombreDelRecurso)
            ?? throw new InvalidOperationException("No se encontró el logo incrustado del reporte.");
        using var memoria = new MemoryStream();
        flujo.CopyTo(memoria);
        return memoria.ToArray();
    }
}
