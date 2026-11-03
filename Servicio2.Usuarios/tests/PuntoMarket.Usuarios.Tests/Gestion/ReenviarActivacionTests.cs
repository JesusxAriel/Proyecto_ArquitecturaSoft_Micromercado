using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.ReenviarActivacion;
using PuntoMarket.Usuarios.Domain.Enums;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ReenviarActivacionTests
{
    private readonly UsuarioGestionRepositorioStub repositorio = new();
    private readonly RelojFijo reloj = new();
    private readonly GeneradorTokenSpy tokens = new();
    private readonly EnviadorCorreoSpy correo = new();

    public ReenviarActivacionTests()
    {
        repositorio.UsuarioParaActivacion = new UsuarioActivacionDto(42, "ana@example.com",
            EstadoUsuario.PENDIENTE, "hash-anterior", reloj.UtcNow.AddMinutes(-1).UtcDateTime, null);
    }

    private ReenviarActivacionCasoDeUso CrearCaso(IUsuarioActual? usuario = null) =>
        new(repositorio, usuario ?? new UsuarioActualStub(), reloj, tokens, correo);

    [Theory]
    [InlineData(7, "Cajero")]
    [InlineData(7, "")]
    [InlineData(0, "Administrador")]
    public async Task Solo_administrador_identificado_puede_reenviar(int id, string rol)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCaso(new UsuarioActualStub(id, rol)).EjecutarAsync(new(42)));

        Assert.Empty(repositorio.Llamadas);
        Assert.Empty(tokens.Solicitudes);
        Assert.Empty(correo.Envios);
    }

    [Theory]
    [InlineData(EstadoUsuario.ACTIVO, true)]
    [InlineData(EstadoUsuario.ACTIVO, false)]
    [InlineData(EstadoUsuario.INACTIVO, false)]
    [InlineData(EstadoUsuario.PENDIENTE, true)]
    public async Task Rechaza_cuenta_que_no_es_pendiente_o_ya_tiene_fecha_de_activacion(
        EstadoUsuario estado, bool activada)
    {
        repositorio.UsuarioParaActivacion = repositorio.UsuarioParaActivacion! with
        {
            Estado = estado,
            FechaActivacion = activada ? reloj.UtcNow.AddDays(-1).UtcDateTime : null
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(new(42)));

        Assert.Empty(tokens.Solicitudes);
        Assert.Empty(repositorio.Renovaciones);
        Assert.Empty(correo.Envios);
    }

    [Fact]
    public async Task Rechaza_usuario_inexistente()
    {
        repositorio.UsuarioParaActivacion = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(new(42)));

        Assert.Empty(tokens.Solicitudes);
        Assert.Empty(repositorio.Renovaciones);
        Assert.Empty(correo.Envios);
    }

    [Fact]
    public async Task Genera_token_nuevo_y_reemplaza_hash_anterior_mediante_repositorio()
    {
        var hashAnterior = repositorio.UsuarioParaActivacion!.TokenActivacionHash;

        var usuarioActual = new UsuarioActualStub(19);
        var salida = await CrearCaso(usuarioActual).EjecutarAsync(new(42));

        Assert.True(salida.Reenviado);
        var token = Assert.Single(tokens.Tokens);
        Assert.NotEqual(hashAnterior, token.Hash);
        Assert.Equal((reloj.UtcNow, TimeSpan.FromMinutes(10)), Assert.Single(tokens.Solicitudes));
        Assert.Equal((42, token.Hash, reloj.UtcNow.AddMinutes(10).UtcDateTime, usuarioActual.Id),
            Assert.Single(repositorio.Renovaciones));
    }

    [Fact]
    public async Task Envia_token_raw_nuevo_al_correo_devuelto_por_repositorio_despues_de_renovar()
    {
        repositorio.CorreoRenovacion = "actualizado@example.com";
        correo.AlEnviar = () => Assert.Single(repositorio.Renovaciones);

        await CrearCaso().EjecutarAsync(new(42));

        var token = Assert.Single(tokens.Tokens);
        var envio = Assert.Single(correo.Envios);
        Assert.Equal((repositorio.CorreoRenovacion, token.Valor, token.Expira), envio);
        Assert.NotEqual(token.Hash, envio.Token);
    }

    [Fact]
    public async Task Cada_reenvio_solicita_y_persiste_un_token_nuevo()
    {
        var caso = CrearCaso();

        await caso.EjecutarAsync(new(42));
        await caso.EjecutarAsync(new(42));

        Assert.Equal(2, tokens.Tokens.Count);
        Assert.NotEqual(tokens.Tokens[0].Valor, tokens.Tokens[1].Valor);
        Assert.Equal(tokens.Tokens.Select(token => token.Hash), repositorio.Renovaciones.Select(renovacion => renovacion.Hash));
        Assert.Equal(tokens.Tokens.Select(token => token.Valor), correo.Envios.Select(envio => envio.Token));
    }

    [Fact]
    public async Task Falla_sin_enviar_correo_si_renovacion_devuelve_null()
    {
        repositorio.CorreoRenovacion = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(new(42)));

        Assert.Single(repositorio.Renovaciones);
        Assert.Empty(correo.Envios);
    }
}
