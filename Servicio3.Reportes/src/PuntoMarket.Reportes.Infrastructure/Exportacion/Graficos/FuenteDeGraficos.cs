using SkiaSharp;

namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Graficos;

internal static class FuenteDeGraficos
{
    private static readonly string[] Preferidas = ["Segoe UI", "DejaVu Sans", "Liberation Sans", "Arial"];

    public static string Nombre { get; } = Elegir();

    private static string Elegir()
    {
        var instaladas = SKFontManager.Default.GetFontFamilies().ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Preferidas.FirstOrDefault(instaladas.Contains) ?? SKTypeface.Default.FamilyName;
    }
}
