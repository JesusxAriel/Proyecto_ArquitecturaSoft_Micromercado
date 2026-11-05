using System.Text.RegularExpressions;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class FormularioVentasListadoTests
{
    private static string Vista() => Leer("Pages", "Reportes", "VentasListado.cshtml");

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
    [InlineData("Filtros.Estado")]
    [InlineData("Filtros.Cliente")]
    [InlineData("Filtros.Tope")]
    public void Los_campos_de_filtro_no_declaran_restricciones_html_que_generan_mensajes_en_ingles(string campo)
    {
        var entrada = Regex.Match(Vista(), $@"<(input|select) asp-for=""{Regex.Escape(campo)}""[^>]*>");

        Assert.True(entrada.Success);
        Assert.DoesNotMatch(@"\b(min|max|step|pattern|required|minlength|maxlength)\s*=", entrada.Value);
    }

    [Theory]
    [InlineData("Listado de ventas")]
    [InlineData("Razón social")]
    [InlineData("NIT/CI")]
    [InlineData("Sumariados")]
    [InlineData("Descargar PDF")]
    [InlineData("Descargar Excel")]
    [InlineData("Sin resultados para los filtros seleccionados")]
    [InlineData("Monto total de las completadas")]
    [InlineData("Promedio por venta completada")]
    public void La_pantalla_muestra_los_textos_con_su_ortografia(string texto)
    {
        Assert.Contains(texto, Vista());
    }

    [Fact]
    public void La_pantalla_ofrece_las_dos_descargas_conservando_los_filtros()
    {
        var vista = Vista();

        Assert.Contains(@"asp-page-handler=""DescargarPdf"" asp-all-route-data=""Model.ParametrosDeConsulta""", vista);
        Assert.Contains(@"asp-page-handler=""DescargarExcel"" asp-all-route-data=""Model.ParametrosDeConsulta""", vista);
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
