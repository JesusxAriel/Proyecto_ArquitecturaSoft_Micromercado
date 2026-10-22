using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ArquitecturaHexagonalTests
{
    private const string Raiz = "Proyecto_Arquitectura_Micromercado";
    private const string NsDominio = Raiz + ".Domain";
    private const string NsAplicacion = Raiz + ".Application";
    private const string NsInfraestructura = Raiz + ".Infrastructure";
    private const string NsPaginas = Raiz + ".Pages";

    [Fact]
    public void Administracion_de_usuarios_tiene_puerto_propio_y_no_amplia_autenticacion()
    {
        var autenticacion = typeof(Proyecto_Arquitectura_Micromercado.Application.Security.IUsuarioRepository);
        var administracion = typeof(Proyecto_Arquitectura_Micromercado.Application.Users.IUsuarioAdministracionRepository);
        var servicio = typeof(Proyecto_Arquitectura_Micromercado.Application.Users.UsuarioAdministracionService);
        Assert.Equal(
            ["ActualizarPasswordAsync", "EstaActivoAsync", "ObtenerCredencialesAsync", "ObtenerCredencialesPorIdAsync"],
            autenticacion.GetMethods().Select(metodo => metodo.Name).OrderBy(nombre => nombre).ToArray());
        Assert.False(autenticacion.IsAssignableFrom(administracion));
        Assert.Contains(servicio.GetConstructors().Single().GetParameters(), p => p.ParameterType == administracion);
        Assert.DoesNotContain(servicio.GetConstructors().Single().GetParameters(), p => p.ParameterType == autenticacion);
        // También cubre cualquier futura Pages/Users, sin depender de que exista hoy.
        Pages_no_depende_de_repositorios();
    }

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

    [Fact]
    public void Pages_no_depende_del_driver_de_base_de_datos()
    {
        AssertSinDependenciasHacia(
            capa: "Pages",
            prohibidos: ["MySql.Data"],
            porque: "La UI debe manejar errores de persistencia sin saber que motor hay detras: " +
                    "con el motor en memoria estos catch nunca se disparan.");
    }

    [Fact]
    public void Domain_Suppliers_no_usa_DataAnnotations()
    {
        AssertSinDependenciasHacia(
            capa: Path.Combine("Domain", "Suppliers"),
            prohibidos: ["System.ComponentModel.DataAnnotations"],
            porque: "Las entidades del dominio no llevan validacion de pantalla. Esos atributos " +
                    "van en el modelo del formulario (Pages/Suppliers/SupplierFormModel).");
    }

    [Fact]
    public void Pages_no_referencia_Infrastructure_ni_el_motor_de_base_de_datos()
    {
        AssertPaginasSinPatrones(
            patrones:
            [
                new Regex(Regex.Escape(NsInfraestructura), RegexOptions.CultureInvariant),
                new Regex(@"\bMySql\w*", RegexOptions.CultureInvariant),
                new Regex(@"\bDatabaseConnection\b", RegexOptions.CultureInvariant)
            ],
            porque: "La UI es el adaptador de entrada: habla con Application y no conoce a los " +
                    "adaptadores de salida (Infrastructure) ni al motor que hay detras.");
    }

    [Fact]
    public void Pages_no_depende_de_repositorios()
    {
        AssertPaginasSinPatrones(
            patrones: [new Regex(@"\bI[A-Z]\w*Repository\b", RegexOptions.CultureInvariant)],
            porque: "Los repositorios son puertos de salida de Application. Una Page debe pedir " +
                    "un servicio o una fachada, nunca saltarse la capa de aplicacion para leer " +
                    "o escribir datos.");
    }

    [Fact]
    public void Pages_no_consume_la_fachada_de_stock()
    {
        AssertPaginasSinPatrones(
            patrones: [new Regex(@"\bIFachadaActualizacionStockProducto\b", RegexOptions.CultureInvariant)],
            porque: "La fachada de stock es de uso interno de las fachadas de venta y anulacion. " +
                    "Las paginas solo deben conocer IFachadaVenta, IFachadaAnulacion y los " +
                    "servicios de consulta.");
    }

    private static void AssertPaginasSinPatrones(Regex[] patrones, string porque)
    {
        var carpeta = Path.Combine(RaizDelProyecto(), "Pages");

        Assert.True(Directory.Exists(carpeta), $"No se encontro la carpeta Pages en {carpeta}.");

        var infracciones = new List<string>();
        var archivos = Directory.EnumerateFiles(carpeta, "*.*", SearchOption.AllDirectories)
            .Where(ruta => ruta.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           ruta.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));

        foreach (var archivo in archivos)
        {
            var lineas = File.ReadAllLines(archivo);

            for (var i = 0; i < lineas.Length; i++)
            {
                var linea = lineas[i].TrimStart();

                if (linea.StartsWith("//", StringComparison.Ordinal) ||
                    linea.StartsWith("@*", StringComparison.Ordinal))
                {
                    continue;
                }

                if (patrones.Any(patron => patron.IsMatch(linea)))
                {
                    infracciones.Add(
                        $"  {Path.GetRelativePath(RaizDelProyecto(), archivo)}:{i + 1} -> {linea.Trim()}");
                }
            }
        }

        Assert.True(
            infracciones.Count == 0,
            $"""
            Pages tiene {infracciones.Count} referencia(s) prohibida(s).

            {porque}

            {string.Join(Environment.NewLine, infracciones)}
            """);
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

    private static string? DirectivaUsing(string linea)
    {
        var coincidencia = Regex.Match(
            linea.Trim(),
            @"^using\s+(?:static\s+)?(?<ns>[A-Za-z_][\w.]*)\s*;",
            RegexOptions.CultureInvariant);

        return coincidencia.Success ? coincidencia.Groups["ns"].Value : null;
    }

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
