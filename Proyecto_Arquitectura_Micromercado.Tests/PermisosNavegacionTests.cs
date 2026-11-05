using System.Security.Claims;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public sealed class PermisosNavegacionTests
{
    [Fact]
    public void Administrador_ve_opciones_administrativas_y_caja_ventas()
    {
        var permisos = PermisosNavegacion.Desde(CrearUsuario("Administrador"));

        Assert.True(permisos.PuedeAdministrar);
        Assert.True(permisos.PuedeAccederCajaVentas);
        Assert.False(permisos.PuedeConsultarCatalogo);
    }

    [Fact]
    public void Cajero_ve_caja_ventas_y_catalogo_sin_opciones_administrativas()
    {
        var permisos = PermisosNavegacion.Desde(CrearUsuario("Cajero"));

        Assert.False(permisos.PuedeAdministrar);
        Assert.True(permisos.PuedeAccederCajaVentas);
        Assert.True(permisos.PuedeConsultarCatalogo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("RolDesconocido")]
    public void Usuario_sin_rol_reconocido_no_ve_opciones_del_menu(string? rol)
    {
        var permisos = PermisosNavegacion.Desde(CrearUsuario(rol));

        Assert.False(permisos.PuedeAdministrar);
        Assert.False(permisos.PuedeAccederCajaVentas);
        Assert.False(permisos.PuedeConsultarCatalogo);
    }

    private static ClaimsPrincipal CrearUsuario(string? rol)
    {
        var claims = rol is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.Role, rol)];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie"));
    }
}
