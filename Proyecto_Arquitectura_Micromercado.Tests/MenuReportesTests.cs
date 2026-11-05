using System.Security.Claims;
using System.Text.RegularExpressions;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class MenuReportesTests
{
    [Fact]
    public void El_desplegable_de_reportes_lista_el_listado_antes_que_ventas_por_categoria()
    {
        var contenido = Layout();

        var listado = contenido.IndexOf("asp-page=\"/Reportes/VentasListado\">Listado de ventas</a>", StringComparison.Ordinal);
        var categoria = contenido.IndexOf("asp-page=\"/Reportes/ResumenCategorias\">Ventas por categoría</a>", StringComparison.Ordinal);

        Assert.True(listado > 0);
        Assert.True(listado < categoria);
    }

    [Fact]
    public void La_entrada_del_listado_esta_dentro_del_bloque_exclusivo_del_administrador()
    {
        var contenido = Layout();

        var inicioAdministrador = contenido.IndexOf("@if (esAdministrador)", StringComparison.Ordinal);
        var inicioCaja = contenido.IndexOf("@if (permisosNavegacion.PuedeAccederCajaVentas)", StringComparison.Ordinal);
        var listado = contenido.IndexOf("/Reportes/VentasListado", StringComparison.Ordinal);

        Assert.InRange(listado, inicioAdministrador + 1, inicioCaja - 1);
        Assert.Single(Regex.Matches(contenido, "/Reportes/VentasListado"));
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Cajero", false)]
    public void Solo_el_administrador_accede_al_bloque_que_contiene_los_reportes(string rol, bool esperado)
    {
        var usuario = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, rol)], "Prueba"));

        Assert.Equal(esperado, PermisosNavegacion.Desde(usuario).PuedeAdministrar);
    }

    private static string Layout()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Proyecto_Arquitectura_Micromercado.slnx")))
            directorio = directorio.Parent;

        return File.ReadAllText(Path.Combine(
            directorio?.FullName ?? throw new DirectoryNotFoundException("No se encontro la raiz del repositorio."),
            "Proyecto_Arquitectura_Micromercado", "Pages", "Shared", "_Layout.cshtml"));
    }
}
