using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Hace verificable la regla de dependencia de la arquitectura hexagonal.
//
// Las capas del proyecto son carpetas dentro de un mismo ensamblado, no proyectos
// separados, asi que el compilador NO impide que alguien escriba un using prohibido.
// Estas pruebas ocupan ese lugar: si una dependencia apunta hacia afuera en vez de
// hacia adentro, la suite falla y dice exactamente en que archivo y linea.
public class ArquitecturaHexagonalTests
{
    // Namespaces del propio proyecto, por capa.
    private const string Raiz = "Proyecto_Arquitectura_Micromercado";
    private const string NsDominio = Raiz + ".Domain";
    private const string NsAplicacion = Raiz + ".Application";
    private const string NsInfraestructura = Raiz + ".Infrastructure";
    private const string NsPaginas = Raiz + ".Pages";

    [Fact]
    public void Domain_no_depende_de_ninguna_otra_capa()
    {
        AssertSinDependenciasHacia(
            capa: "Domain",
            prohibidos: [NsAplicacion, NsInfraestructura, NsPaginas, "MySql.Data", "Microsoft.AspNetCore"],
            porque: "El dominio es el centro del hexagono: no puede conocer a nadie.");
    }

    [Fact]
    public void Application_no_depende_de_Infrastructure_ni_de_la_UI()
    {
        AssertSinDependenciasHacia(
            capa: "Application",
            prohibidos: [NsInfraestructura, NsPaginas, "MySql.Data", "Microsoft.AspNetCore"],
            porque: "Application define los puertos; los adaptadores dependen de ella, " +
                    "nunca al reves. Si aparece MySql.Data aca, un puerto esta filtrando " +
                    "un detalle de persistencia.");
    }

    [Fact]
    public void Infrastructure_no_depende_de_la_UI()
    {
        AssertSinDependenciasHacia(
            capa: "Infrastructure",
            prohibidos: [NsPaginas],
            porque: "Los adaptadores de salida no pueden conocer al adaptador de entrada.");
    }

    // Estuvo deshabilitada mientras las Pages capturaban MySqlException en 8 lugares, o sea
    // mientras el adaptador de entrada dependia del driver del adaptador de salida. Ya se
    // corrigio: los adaptadores traducen sus errores a ErrorDePersistenciaException y la UI
    // captura esa. Si alguien vuelve a capturar el driver en una pagina, esta prueba falla.
    [Fact]
    public void Pages_no_depende_del_driver_de_base_de_datos()
    {
        AssertSinDependenciasHacia(
            capa: "Pages",
            prohibidos: ["MySql.Data"],
            porque: "La UI debe manejar errores de persistencia sin saber que motor hay detras: " +
                    "con el motor en memoria estos catch nunca se disparan.");
    }

    private static void AssertSinDependenciasHacia(
        string capa,
        string[] prohibidos,
        string porque)
    {
        var carpeta = Path.Combine(RaizDelProyecto(), capa);

        Assert.True(
            Directory.Exists(carpeta),
            $"No se encontro la carpeta de la capa '{capa}' en {carpeta}.");

        var infracciones = new List<string>();

        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*.cs", SearchOption.AllDirectories))
        {
            var lineas = File.ReadAllLines(archivo);

            for (var i = 0; i < lineas.Length; i++)
            {
                var directiva = DirectivaUsing(lineas[i]);

                if (directiva is null)
                {
                    continue;
                }

                foreach (var prohibido in prohibidos)
                {
                    if (directiva == prohibido ||
                        directiva.StartsWith(prohibido + ".", StringComparison.Ordinal))
                    {
                        infracciones.Add(
                            $"  {Path.GetRelativePath(RaizDelProyecto(), archivo)}:{i + 1} -> using {directiva};");
                    }
                }
            }
        }

        Assert.True(
            infracciones.Count == 0,
            $"""
            La capa '{capa}' tiene {infracciones.Count} dependencia(s) prohibida(s).

            {porque}

            {string.Join(Environment.NewLine, infracciones)}
            """);
    }

    // Devuelve el namespace de un "using X.Y;" o null si la linea no es una directiva using.
    // Ignora alias, using static y los using dentro de un bloque (indentados con sangria),
    // que en este proyecto no existen pero no deben romper el analisis.
    private static string? DirectivaUsing(string linea)
    {
        var coincidencia = Regex.Match(
            linea.Trim(),
            @"^using\s+(?:static\s+)?(?<ns>[A-Za-z_][\w.]*)\s*;",
            RegexOptions.CultureInvariant);

        return coincidencia.Success ? coincidencia.Groups["ns"].Value : null;
    }

    // Sube desde el directorio de ejecucion hasta encontrar la solucion, y entra al
    // proyecto de la aplicacion. Asi la prueba no depende de rutas relativas fragiles.
    private static string RaizDelProyecto()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null &&
               !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        Assert.NotNull(directorio);

        return Path.Combine(directorio.FullName, Raiz);
    }
}
