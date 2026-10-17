using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class MensajesDeVentasTests
{
    private static readonly string[] ArchivosDeLaPantalla =
    [
        Path.Combine("wwwroot", "js", "ventas.js"),
        Path.Combine("wwwroot", "js", "ventas-reglas.js"),
        Path.Combine("Pages", "Sales", "Index.cshtml"),
        Path.Combine("Pages", "Sales", "Index.cshtml.cs"),
        Path.Combine("Pages", "Sales", "SaleInputModel.cs")
    ];

    [Fact]
    public void Todos_los_mensajes_de_cantidad_usan_la_misma_redaccion()
    {
        var textos = Textos().ToList();

        foreach (var (_, contenido) in textos)
        {
            Assert.DoesNotContain("superior a 0", contenido);
            Assert.DoesNotContain("una cantidad mayor a cero", contenido);
        }

        var apariciones = textos.Sum(t => Regex.Matches(t.Contenido, "Ingrese una cantidad entera mayor a cero[.]").Count);
        Assert.True(apariciones >= 3);
    }

    [Fact]
    public void Los_textos_de_progreso_usan_siempre_el_caracter_de_puntos_suspensivos()
    {
        foreach (var (archivo, contenido) in Textos())
        {
            Assert.False(
                Regex.IsMatch(contenido, @"[A-Za-zÁ-ú]\.\.\.[""<]"),
                $"{archivo} usa tres puntos en lugar de …");
        }
    }

    private static IEnumerable<(string Archivo, string Contenido)> Textos()
    {
        var raiz = RaizDelProyecto();
        return ArchivosDeLaPantalla.Select(archivo => (archivo, File.ReadAllText(Path.Combine(raiz, archivo))));
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
