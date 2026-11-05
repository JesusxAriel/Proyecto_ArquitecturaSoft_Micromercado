using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.UseCases.Gestion.CrearUsuario;
using PuntoMarket.Usuarios.Domain.Exceptions;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class CrearUsuarioTests
{
    private readonly UsuarioGestionRepositorioStub repositorio = new();
    private readonly RelojFijo reloj = new();
    private readonly HasheadorPasswordSpy hasheador = new();
    private readonly GeneradorTokenSpy tokens = new();
    private readonly EnviadorCorreoSpy correo = new();
    private static readonly CrearUsuarioEntrada Entrada = new("Ana", "Perez", "ana@example.com", 2);

    private CrearUsuarioCasoDeUso CrearCaso(IUsuarioActual? usuario = null) =>
        new(repositorio, usuario ?? new UsuarioActualStub(), hasheador, reloj, tokens, correo);

    [Theory]
    [InlineData("Ana", "Perez", "aperez")]
    [InlineData("  María  Elena ", "  López  Vargas ", "mlopez")]
    [InlineData("JOSÉ", "NÚÑEZ", "jnunez")]
    public async Task Genera_username_con_inicial_y_primer_apellido_sin_tildes(
        string nombre, string apellido, string esperado)
    {
        var salida = await CrearCaso().EjecutarAsync(Entrada with { Nombre = nombre, Apellido = apellido });

        Assert.Equal(esperado, Assert.Single(repositorio.Altas).Username);
        Assert.Equal(repositorio.IdCreado, salida.Id);
    }

    [Fact]
    public async Task Agrega_sufijo_hasta_encontrar_username_disponible()
    {
        repositorio.UsernamesExistentes.UnionWith(["aperez", "aperez1"]);

        await CrearCaso().EjecutarAsync(Entrada);

        Assert.Equal(["aperez", "aperez1", "aperez2"], repositorio.UsernamesConsultados);
        Assert.Equal("aperez2", Assert.Single(repositorio.Altas).Username);
    }

    [Fact]
    public async Task Rechaza_correo_duplicado_sin_crear_ni_enviar_correo()
    {
        repositorio.ExisteCorreo = true;

        await Assert.ThrowsAsync<UsuarioDuplicadoException>(() => CrearCaso().EjecutarAsync(Entrada));

        Assert.Empty(repositorio.Altas);
        Assert.Empty(tokens.Solicitudes);
        Assert.Empty(correo.Envios);
    }

    [Fact]
    public async Task Rechaza_rol_inactivo_sin_crear_ni_enviar_correo()
    {
        repositorio.RolActivo = false;

        await Assert.ThrowsAsync<ArgumentException>(() => CrearCaso().EjecutarAsync(Entrada));

        Assert.Empty(repositorio.Altas);
        Assert.Empty(tokens.Solicitudes);
        Assert.Empty(correo.Envios);
    }

    [Theory]
    [InlineData(7, "Cajero")]
    [InlineData(7, "")]
    [InlineData(0, "Administrador")]
    public async Task Rechaza_usuario_sin_autorizacion_antes_de_acceder_al_repositorio(int id, string rol)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearCaso(new UsuarioActualStub(id, rol)).EjecutarAsync(Entrada));

        Assert.Empty(repositorio.Llamadas);
        Assert.Empty(correo.Envios);
    }

    [Fact]
    public async Task Envia_correo_con_token_raw_despues_de_crear()
    {
        correo.AlEnviar = () => Assert.Single(repositorio.Altas);

        await CrearCaso().EjecutarAsync(Entrada);

        var token = Assert.Single(tokens.Tokens);
        var envio = Assert.Single(correo.Envios);
        Assert.Equal((Entrada.Correo, token.Valor, token.Expira), envio);
        Assert.Equal(token.Hash, Assert.Single(repositorio.Altas).TokenActivacionHash);
        Assert.NotEqual(token.Hash, envio.Token);
    }

    [Fact]
    public async Task No_envia_correo_si_la_creacion_falla()
    {
        repositorio.FallarCreacion = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(Entrada));

        Assert.Empty(correo.Envios);
    }

    [Fact]
    public async Task Solicita_y_persiste_token_con_vigencia_de_diez_minutos()
    {
        await CrearCaso().EjecutarAsync(Entrada);

        Assert.Equal((reloj.UtcNow, TimeSpan.FromMinutes(10)), Assert.Single(tokens.Solicitudes));
        Assert.Equal(reloj.UtcNow.AddMinutes(10).UtcDateTime,
            Assert.Single(repositorio.Altas).TokenActivacionExpira);
        Assert.Equal(reloj.UtcNow.AddMinutes(10), Assert.Single(correo.Envios).Expira);
    }
}
