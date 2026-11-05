using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Arquitectura;

public class ArquitecturaCleanTests
{
    private const string Raiz = "PuntoMarket.Usuarios";

    [Fact]
    public void Domain_no_depende_de_otras_capas_ni_de_infraestructura_externa()
    {
        AssertSinDependenciasHacia("Domain",
            [Raiz + ".Application", Raiz + ".Infrastructure", Raiz + ".Api",
             "MySql.Data", "Microsoft.AspNetCore"]);
    }

    [Fact]
    public void Application_no_depende_de_Infrastructure_Api_ni_de_infraestructura_externa()
    {
        AssertSinDependenciasHacia("Application",
            [Raiz + ".Infrastructure", Raiz + ".Api", "MySql.Data", "Microsoft.AspNetCore"]);
    }

    [Fact]
    public void Infrastructure_no_depende_de_Api()
    {
        AssertSinDependenciasHacia("Infrastructure", [Raiz + ".Api"]);
    }

    [Fact]
    public void Las_clases_CasoDeUso_pertenecen_a_Application_UseCases()
    {
        var infracciones = new List<string>();
        foreach (var capa in new[] { "Domain", "Application", "Infrastructure", "Api" })
        {
            foreach (var archivo in ArchivosDeCapa(capa))
            {
                var contenido = File.ReadAllText(archivo);
                var espacio = Regex.Match(contenido, @"\bnamespace\s+(?<ns>[\w.]+)\s*[;{]");
                foreach (Match clase in Regex.Matches(contenido, @"\bclass\s+(?<nombre>\w*CasoDeUso)\b"))
                {
                    if (!espacio.Groups["ns"].Value.Contains("Application.UseCases", StringComparison.Ordinal))
                        infracciones.Add($"{archivo}: {clase.Groups["nombre"].Value}");
                }
            }
        }

        Assert.True(infracciones.Count == 0,
            "Las clases CasoDeUso deben vivir en Application.UseCases." +
            Environment.NewLine + string.Join(Environment.NewLine, infracciones));
    }

    private static void AssertSinDependenciasHacia(string capa, string[] prohibidos)
    {
        var carpeta = CarpetaDeCapa(capa);
        var proyecto = XDocument.Load(Path.Combine(carpeta, $"{Raiz}.{capa}.csproj"));
        var referencias = proyecto.Descendants()
            .Where(elemento => elemento.Name.LocalName is
                "ProjectReference" or "PackageReference" or "FrameworkReference")
            .Select(elemento => (string?)elemento.Attribute("Include") ?? "");
        var infracciones = new List<string>();

        foreach (var referencia in referencias)
        {
            if (prohibidos.Any(prohibido => referencia.Contains(prohibido, StringComparison.Ordinal)))
                infracciones.Add($"{capa}.csproj -> {referencia}");
        }

        foreach (var archivo in ArchivosDeCapa(capa))
        {
            var lineas = File.ReadAllLines(archivo);
            for (var i = 0; i < lineas.Length; i++)
            {
                if (lineas[i].TrimStart().StartsWith("//", StringComparison.Ordinal))
                    continue;

                if (prohibidos.Any(prohibido => Regex.IsMatch(lineas[i],
                    @"\b" + Regex.Escape(prohibido) + @"\b", RegexOptions.CultureInvariant)))
                    infracciones.Add($"{archivo}:{i + 1} -> {lineas[i].Trim()}");
            }
        }

        Assert.True(infracciones.Count == 0,
            $"La capa {capa} tiene dependencias prohibidas." +
            Environment.NewLine + string.Join(Environment.NewLine, infracciones));
    }

    private static IEnumerable<string> ArchivosDeCapa(string capa) =>
        Directory.EnumerateFiles(CarpetaDeCapa(capa), "*.cs", SearchOption.AllDirectories)
            .Where(archivo => !Path.GetRelativePath(CarpetaDeCapa(capa), archivo)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segmento => segmento is "bin" or "obj"));

    private static string CarpetaDeCapa(string capa)
    {
        var carpeta = Path.Combine(RaizDelServicio(), "src", $"{Raiz}.{capa}");
        Assert.True(Directory.Exists(carpeta), $"No se encontro la capa {capa} en {carpeta}.");
        return carpeta;
    }

    private static string RaizDelServicio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null &&
               !File.Exists(Path.Combine(directorio.FullName, "PuntoMarket.Usuarios.sln")))
            directorio = directorio.Parent;

        Assert.NotNull(directorio);
        return directorio.FullName;
    }
}
