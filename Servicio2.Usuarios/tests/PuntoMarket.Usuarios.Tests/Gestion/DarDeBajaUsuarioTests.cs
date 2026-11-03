using PuntoMarket.Usuarios.Application.UseCases.Gestion.DarDeBajaUsuario;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class DarDeBajaUsuarioTests
{
    [Theory]
    [InlineData(7, "Cajero")]
    [InlineData(7, "")]
    [InlineData(0, "Administrador")]
    public async Task Solo_administrador_identificado_puede_dar_de_baja(int id, string rol)
    {
        var repositorio = new UsuarioGestionRepositorioStub();
        var caso = new DarDeBajaUsuarioCasoDeUso(repositorio, new UsuarioActualStub(id, rol));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => caso.EjecutarAsync(new(42)));

        Assert.Empty(repositorio.Llamadas);
    }

    [Fact]
    public async Task No_permite_autodesactivacion()
    {
        var repositorio = new UsuarioGestionRepositorioStub();
        var usuario = new UsuarioActualStub();
        var caso = new DarDeBajaUsuarioCasoDeUso(repositorio, usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(new(usuario.Id)));

        Assert.Empty(repositorio.Bajas);
    }

    [Fact]
    public async Task Envia_id_del_usuario_actual_como_usuario_de_eliminacion()
    {
        var repositorio = new UsuarioGestionRepositorioStub();
        var usuario = new UsuarioActualStub(19);
        var caso = new DarDeBajaUsuarioCasoDeUso(repositorio, usuario);

        var salida = await caso.EjecutarAsync(new(42));

        Assert.True(salida.BajaRealizada);
        Assert.Equal((42, usuario.Id), Assert.Single(repositorio.Bajas));
    }

    [Fact]
    public async Task Falla_si_repositorio_no_realiza_la_baja()
    {
        var repositorio = new UsuarioGestionRepositorioStub { ResultadoDesactivacion = false };
        var caso = new DarDeBajaUsuarioCasoDeUso(repositorio, new UsuarioActualStub());

        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(new(42)));

        Assert.Single(repositorio.Bajas);
    }
}
