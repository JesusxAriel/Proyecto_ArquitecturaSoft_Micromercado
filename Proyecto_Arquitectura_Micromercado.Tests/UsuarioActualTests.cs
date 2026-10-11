using Microsoft.AspNetCore.Http;
using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;
using Proyecto_Arquitectura_Micromercado.Pages.Shared;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UsuarioActualTests
{
    private static UsuarioActualDesdeClaims ConUsuario(int id, string nombre, string rol)
    {
        var contexto = new DefaultHttpContext
        {
            User = PrincipalDeSesion.Crear(id, nombre, rol, "Cookies")
        };

        return new UsuarioActualDesdeClaims(new HttpContextAccessor { HttpContext = contexto });
    }

    [Fact]
    public void Lee_la_identidad_de_los_claims_de_la_cookie()
    {
        var usuario = ConUsuario(7, "cajero1", Roles.Cajero);

        Assert.Equal(7, usuario.Id);
        Assert.Equal("cajero1", usuario.NombreUsuario);
        Assert.Equal(Roles.Cajero, usuario.Rol);
    }

    [Fact]
    public void Sin_usuario_autenticado_lanza_en_vez_de_devolver_un_autor_falso()
    {
        var sinLogin = new UsuarioActualDesdeClaims(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Assert.Throws<InvalidOperationException>(() => sinLogin.Id);
        Assert.Throws<InvalidOperationException>(() => sinLogin.NombreUsuario);
        Assert.Throws<InvalidOperationException>(() => sinLogin.Rol);
    }

    [Fact]
    public void Sin_contexto_http_lanza()
    {
        var sinContexto = new UsuarioActualDesdeClaims(new HttpContextAccessor());

        Assert.Throws<InvalidOperationException>(() => sinContexto.Id);
    }

    [Fact]
    public async Task La_categoria_creada_queda_auditada_con_el_usuario_actual()
    {
        var repositorio = new InMemoryCategoryRepository(new UsuarioActualEnMemoria(id: 5));
        var categoria = new Category
        {
            Name = "Lacteos",
            Code = "CAT-LAC",
            AisleLocation = "Pasillo 1"
        };

        var id = await repositorio.CreateAsync(categoria);
        var guardada = await repositorio.GetByIdAsync(id);

        Assert.NotNull(guardada);
        Assert.Equal(5, guardada.AdminUserId);
    }

    [Fact]
    public async Task Editar_una_categoria_no_cambia_quien_la_creo()
    {
        var creador = new InMemoryCategoryRepository(new UsuarioActualEnMemoria(id: 5));
        var id = await creador.CreateAsync(new Category
        {
            Name = "Lacteos",
            Code = "CAT-LAC",
            AisleLocation = "Pasillo 1"
        });

        // La edicion no debe sobrescribir al autor de la creacion.
        var editada = await creador.GetByIdAsync(id);
        Assert.NotNull(editada);
        editada.Description = "Leches y yogures";
        await creador.UpdateAsync(editada);

        var guardada = await creador.GetByIdAsync(id);
        Assert.NotNull(guardada);
        Assert.Equal(5, guardada.AdminUserId);
    }
}
