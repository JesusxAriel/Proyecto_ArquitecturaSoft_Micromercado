using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class AccesibilidadDelLayoutTests
{
    [Fact]
    public void El_enlace_del_logo_tiene_nombre_accesible_y_la_imagen_es_decorativa()
    {
        var contenido = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Shared", "_Layout.cshtml"));

        var enlace = Regex.Match(contenido, @"<a class=""navbar-brand""[^>]*>\s*<img[^>]*>", RegexOptions.Singleline);

        Assert.True(enlace.Success);
        Assert.Contains("aria-label=\"PuntoMarket, ir al inicio\"", enlace.Value);
        Assert.Contains("alt=\"\"", enlace.Value);
    }

    private static string RaizDelProyecto()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        return Path.Combine(
            directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio."),
            "Proyecto_Arquitectura_Micromercado");
    }
}
