using Proyecto_Arquitectura_Micromercado.Application.Security;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class AutenticacionServiceTests
{
    private readonly BCryptPasswordHasher hasher = new();
    private readonly AutenticacionService servicio;

    public AutenticacionServiceTests()
    {
        var usuarios = new InMemoryUsuarioRepository()
            .Agregar(new CredencialesDeUsuario(
                1, "admin", "Gerente General", Roles.Administrador, hasher.Hash("admin123"), true))
            .Agregar(new CredencialesDeUsuario(
                2, "cajero1", "Carlos Mamani", Roles.Cajero, hasher.Hash("cajero123"), true))
            .Agregar(new CredencialesDeUsuario(
                3, "baja", "Usuario de baja", Roles.Cajero, hasher.Hash("baja123"), false));

        servicio = new AutenticacionService(usuarios, hasher);
    }

    [Fact]
    public async Task Credenciales_correctas_devuelven_el_usuario_con_su_rol()
    {
        var usuario = await servicio.AutenticarAsync("admin", "admin123");

        Assert.NotNull(usuario);
        Assert.Equal(1, usuario.Id);
        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal("Gerente General", usuario.NombreCompleto);
        Assert.Equal(Roles.Administrador, usuario.Rol);
    }

    [Fact]
    public async Task Cada_usuario_recibe_su_propio_rol()
    {
        var cajero = await servicio.AutenticarAsync("cajero1", "cajero123");

        Assert.NotNull(cajero);
        Assert.Equal(Roles.Cajero, cajero.Rol);
    }

    [Fact]
    public async Task El_nombre_de_usuario_no_distingue_mayusculas_ni_espacios_alrededor()
    {
        var usuario = await servicio.AutenticarAsync("  ADMIN ", "admin123");

        Assert.NotNull(usuario);
        Assert.Equal(1, usuario.Id);
    }

    [Fact]
    public async Task Una_contrasena_incorrecta_no_inicia_sesion()
    {
        Assert.Null(await servicio.AutenticarAsync("admin", "otra-clave"));
    }

    [Fact]
    public async Task La_contrasena_si_distingue_mayusculas()
    {
        Assert.Null(await servicio.AutenticarAsync("admin", "ADMIN123"));
    }

    [Fact]
    public async Task Un_usuario_inexistente_no_inicia_sesion()
    {
        Assert.Null(await servicio.AutenticarAsync("fantasma", "admin123"));
    }

    [Fact]
    public async Task Un_usuario_inactivo_no_inicia_sesion_aunque_la_contrasena_sea_correcta()
    {
        Assert.Null(await servicio.AutenticarAsync("baja", "baja123"));
    }

    [Theory]
    [InlineData("", "admin123")]
    [InlineData("   ", "admin123")]
    [InlineData("admin", "")]
    public async Task Los_datos_vacios_no_inician_sesion(string usuario, string contrasena)
    {
        Assert.Null(await servicio.AutenticarAsync(usuario, contrasena));
    }

    [Fact]
    public async Task Un_hash_corrupto_en_la_base_no_inicia_sesion_ni_lanza_error()
    {
        var usuarios = new InMemoryUsuarioRepository()
            .Agregar(new CredencialesDeUsuario(9, "roto", "Hash roto", Roles.Cajero, "no-es-un-hash", true));
        var servicioConHashRoto = new AutenticacionService(usuarios, hasher);

        Assert.Null(await servicioConHashRoto.AutenticarAsync("roto", "lo-que-sea"));
    }
}
