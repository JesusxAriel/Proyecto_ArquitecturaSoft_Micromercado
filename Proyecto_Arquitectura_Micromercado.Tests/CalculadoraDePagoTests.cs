using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CalculadoraDePagoTests
{
    private static string Leer(params string[] partes) =>
        File.ReadAllText(Path.Combine([RaizDelProyecto(), .. partes])).ReplaceLineEndings("\n");

    [Fact]
    public void El_campo_se_llama_con_cuanto_paga_y_no_es_obligatorio()
    {
        var vista = Leer("Pages", "Sales", "Index.cshtml");

        Assert.Contains("Con cuánto paga (opcional)", vista);
        var campo = Regex.Match(vista, "<input id=\"montoPago\"[^>]*>").Value;
        Assert.NotEmpty(campo);
        Assert.DoesNotContain("required", campo);
        Assert.Contains("Total a cobrar", vista);
        Assert.DoesNotContain("Efectivo pagado", vista);
    }

    [Fact]
    public void El_monto_no_se_envia_al_servidor_ni_bloquea_la_confirmacion()
    {
        var script = Leer("wwwroot", "js", "ventas.js");

        Assert.DoesNotContain("efectivoPagado", script);
        var payload = script[script.IndexOf("const payload", StringComparison.Ordinal)..];
        payload = payload[..payload.IndexOf("};", StringComparison.Ordinal)];
        Assert.DoesNotContain("montoPago", payload);
        Assert.DoesNotContain("validarMontoDePago", script[script.IndexOf("confirmarVenta.addEventListener", StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Tras_registrar_la_venta_el_campo_se_limpia()
    {
        var script = Leer("wwwroot", "js", "ventas.js");
        var reinicio = script[script.IndexOf("function reiniciarVenta", StringComparison.Ordinal)..];
        reinicio = reinicio[..reinicio.IndexOf("\n    }\n", StringComparison.Ordinal)];

        Assert.Contains("montoPago.value = \"\"", reinicio);
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
