using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FormularioResumenCategoriasTests
{
    private static string Vista() => Leer("Pages", "Reportes", "ResumenCategorias.cshtml");

    private static string Script() => Leer("wwwroot", "js", "resumen-categorias.js");

    [Fact]
    public void El_formulario_de_filtros_desactiva_la_validacion_nativa_del_navegador()
    {
        var formulario = Regex.Match(Vista(), @"<form id=""filtrosReporte""[^>]*>");

        Assert.True(formulario.Success);
        Assert.Contains("novalidate", formulario.Value);
    }

    [Theory]
    [InlineData("Filtros.Desde")]
    [InlineData("Filtros.Hasta")]
    [InlineData("Filtros.Top")]
    [InlineData("Filtros.IdCategoria")]
    public void Los_campos_de_filtro_no_declaran_restricciones_html_que_generan_mensajes_en_ingles(string campo)
    {
        var entrada = Regex.Match(Vista(), $@"<(input|select) asp-for=""{Regex.Escape(campo)}""[^>]*>");

        Assert.True(entrada.Success);
        Assert.DoesNotMatch(@"\b(min|max|step|pattern|required|minlength|maxlength)\s*=", entrada.Value);
    }

    [Fact]
    public void El_script_valida_por_su_cuenta_sin_depender_de_la_validacion_nativa()
    {
        var script = Script();

        Assert.DoesNotContain("checkValidity", script);
        Assert.DoesNotContain("reportValidity", script);
        Assert.DoesNotContain(":invalid", script);
        Assert.Contains("El valor de «Máximo de categorías» debe ser un número entero entre 1 y 100.", script);
        Assert.Contains("La fecha «Desde» no puede ser posterior a la fecha «Hasta».", script);
    }

    private static string Leer(params string[] partes) =>
        File.ReadAllText(Path.Combine([RaizDelProyecto(), .. partes]));

    private static string RaizDelProyecto()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
        {
            directorio = directorio.Parent;
        }

        return Path.Combine(
            directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio."),
            "Proyecto_Arquitectura_Micromercado");
    }
}
