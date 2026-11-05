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

    [Fact]
    public void El_menu_del_usuario_es_un_desplegable_accesible_con_las_tres_opciones_en_orden()
    {
        var contenido = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Shared", "_Layout.cshtml"));

        var boton = Regex.Match(contenido, @"<button[^>]*id=""userDropdown""[^>]*>", RegexOptions.Singleline);
        Assert.True(boton.Success);
        Assert.Contains("data-bs-toggle=\"dropdown\"", boton.Value);
        Assert.Contains("aria-expanded=\"false\"", boton.Value);
        Assert.Contains("aria-labelledby=\"userDropdown\"", contenido);

        var perfil = contenido.IndexOf("Mi perfil", StringComparison.Ordinal);
        var contrasena = contenido.IndexOf("Cambiar contraseña", StringComparison.Ordinal);
        var cierre = contenido.IndexOf("Cerrar sesión", StringComparison.Ordinal);
        Assert.InRange(perfil, 0, contrasena - 1);
        Assert.InRange(contrasena, 0, cierre - 1);
        Assert.Equal(contrasena, contenido.LastIndexOf("Cambiar contraseña", StringComparison.Ordinal));
        Assert.DoesNotContain("Inicio:", contenido);
    }

    [Fact]
    public void La_barra_principal_no_dibuja_la_insignia_de_instancia()
    {
        var contenido = File.ReadAllText(Path.Combine(RaizDelProyecto(), "Pages", "Shared", "_Layout.cshtml"));

        Assert.DoesNotContain("Instancia:", contenido);
        Assert.DoesNotContain("ReplicaId", contenido);
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
