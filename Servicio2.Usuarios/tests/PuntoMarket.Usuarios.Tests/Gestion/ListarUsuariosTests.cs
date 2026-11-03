using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ListarUsuarios;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ListarUsuariosTests
{
    [Theory]
    [InlineData(7, "Cajero")]
    [InlineData(7, "")]
    [InlineData(0, "Administrador")]
    public async Task Solo_administrador_identificado_puede_listar(int id, string rol)
    {
        var repositorio = new UsuarioGestionRepositorioStub();
        var caso = new ListarUsuariosCasoDeUso(repositorio, new UsuarioActualStub(id, rol), new RelojFijo());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => caso.EjecutarAsync(new()));

        Assert.Empty(repositorio.Llamadas);
    }

    [Theory]
    [InlineData("PENDIENTE", -1, true)]
    [InlineData("PENDIENTE", 0, true)]
    [InlineData("PENDIENTE", 1, false)]
    [InlineData("PENDIENTE", null, false)]
    [InlineData("ACTIVO", -1, false)]
    [InlineData("ACTIVO", 0, false)]
    [InlineData("INACTIVO", -1, false)]
    public async Task Calcula_expiracion_solo_para_pendientes_con_token_vencido(
        string estado, int? segundosHastaExpirar, bool esperado)
    {
        var reloj = new RelojFijo();
        DateTime? expira = segundosHastaExpirar is int segundos
            ? reloj.UtcNow.AddSeconds(segundos).UtcDateTime : null;
        var usuario = new UsuarioListadoDto(42, "Ana", "Perez", "ana@example.com", "aperez",
            "Cajero", estado, reloj.UtcNow.AddDays(-1).UtcDateTime, expira);
        var repositorio = new UsuarioGestionRepositorioStub { Usuarios = [usuario] };
        var caso = new ListarUsuariosCasoDeUso(repositorio, new UsuarioActualStub(), reloj);

        var salida = await caso.EjecutarAsync(new());

        var listado = Assert.Single(salida.Usuarios);
        Assert.Equal(esperado, listado.EstaExpirado);
        Assert.Equal(esperado ? "Expirado" : estado, listado.Estado);
        Assert.Equal(expira, listado.TokenActivacionExpira);
        Assert.Equal(usuario.Id, listado.Id);
    }
}
