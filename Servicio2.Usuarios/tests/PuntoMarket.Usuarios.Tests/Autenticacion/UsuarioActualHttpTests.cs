using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PuntoMarket.Usuarios.Api.Security;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public class UsuarioActualHttpTests
{
    [Fact]
    public void Lee_id_y_rol_desde_claims_del_usuario_autenticado()
    {
        var usuarioActual = CrearUsuarioActual(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", "42"),
                new Claim("role", "Administrador")
            ],
            authenticationType: "Bearer")));

        Assert.Equal(42, usuarioActual.Id);
        Assert.Equal("Administrador", usuarioActual.Rol);
    }

    [Fact]
    public void Rechaza_claim_sub_ausente()
    {
        var usuarioActual = CrearUsuarioActual(CrearPrincipal(new Claim("role", "Administrador")));

        Assert.Throws<InvalidOperationException>(() => _ = usuarioActual.Id);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-4")]
    public void Rechaza_claim_sub_no_numerico_o_no_positivo(string id)
    {
        var usuarioActual = CrearUsuarioActual(
            CrearPrincipal(new Claim("sub", id), new Claim("role", "Administrador")));

        Assert.Throws<InvalidOperationException>(() => _ = usuarioActual.Id);
    }

    [Fact]
    public void Rechaza_claim_role_ausente()
    {
        var usuarioActual = CrearUsuarioActual(CrearPrincipal(new Claim("sub", "42")));

        Assert.Throws<InvalidOperationException>(() => _ = usuarioActual.Rol);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rechaza_contexto_http_ausente_o_usuario_no_autenticado(bool sinContexto)
    {
        var accessor = new HttpContextAccessor();
        if (!sinContexto)
        {
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim("sub", "42"),
                        new Claim("role", "Administrador")
                    ]))
            };
        }

        var usuarioActual = new UsuarioActualHttp(accessor);

        Assert.Throws<InvalidOperationException>(() => _ = usuarioActual.Id);
        Assert.Throws<InvalidOperationException>(() => _ = usuarioActual.Rol);
    }

    private static ClaimsPrincipal CrearPrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));

    private static UsuarioActualHttp CrearUsuarioActual(ClaimsPrincipal usuario)
    {
        var contexto = new DefaultHttpContext { User = usuario };
        return new UsuarioActualHttp(new HttpContextAccessor { HttpContext = contexto });
    }
}
