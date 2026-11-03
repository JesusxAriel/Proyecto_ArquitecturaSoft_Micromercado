using PuntoMarket.Usuarios.Application.UseCases.Gestion.ActivarCuenta;
using PuntoMarket.Usuarios.Tests.Gestion.TestDoubles;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Gestion;

public class ActivarCuentaTests
{
    private readonly UsuarioGestionRepositorioStub repositorio = new();
    private readonly HasheadorPasswordSpy hasheador = new();
    private readonly RelojFijo reloj = new();
    private readonly GeneradorTokenSpy tokens = new();
    private static readonly ActivarCuentaEntrada Entrada = new("token-raw", "Segura1!", "Segura1!");

    private ActivarCuentaCasoDeUso CrearCaso() => new(repositorio, hasheador, reloj, tokens);

    [Fact]
    public async Task Activa_con_token_valido_usando_hash_y_hora_actual()
    {
        repositorio.TokensVigentes = new() { ["hash-de:token-raw"] = reloj.UtcNow.AddMinutes(1).UtcDateTime };

        var salida = await CrearCaso().EjecutarAsync(Entrada);

        Assert.True(salida.Activada);
        Assert.Equal(Entrada.Token, Assert.Single(tokens.TokensHasheados));
        Assert.Equal(("hash-de:token-raw", reloj.UtcNow.UtcDateTime), Assert.Single(repositorio.Validaciones));
        Assert.Equal(("hash-de:token-raw", "hash-password-prueba", reloj.UtcNow.UtcDateTime),
            Assert.Single(repositorio.Activaciones));
    }

    [Fact]
    public async Task Rechaza_token_invalido_sin_hashear_contrasena_ni_activar()
    {
        repositorio.TokensVigentes = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(Entrada));

        Assert.Equal(("hash-de:token-raw", reloj.UtcNow.UtcDateTime), Assert.Single(repositorio.Validaciones));
        Assert.Empty(hasheador.Contrasenas);
        Assert.Empty(repositorio.Activaciones);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Rechaza_token_expirado_incluso_en_el_instante_de_vencimiento(int segundosHastaExpirar)
    {
        repositorio.TokensVigentes = new()
        {
            ["hash-de:token-raw"] = reloj.UtcNow.AddSeconds(segundosHastaExpirar).UtcDateTime
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearCaso().EjecutarAsync(Entrada));

        Assert.Equal(("hash-de:token-raw", reloj.UtcNow.UtcDateTime), Assert.Single(repositorio.Validaciones));
        Assert.Empty(repositorio.Activaciones);
        Assert.Empty(hasheador.Contrasenas);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Aa1!", "Aa1!")]
    [InlineData("segura1!", "segura1!")]
    [InlineData("SEGURA1!", "SEGURA1!")]
    [InlineData("Seguraaa!", "Seguraaa!")]
    [InlineData("Segura12", "Segura12")]
    [InlineData("Segura1!", "Distinta1!")]
    public async Task Rechaza_contrasena_que_incumple_politica_o_confirmacion(string contrasena, string confirmacion)
    {
        var entrada = Entrada with { NuevaContrasena = contrasena, ConfirmarContrasena = confirmacion };

        await Assert.ThrowsAsync<ArgumentException>(() => CrearCaso().EjecutarAsync(entrada));

        Assert.Empty(repositorio.Llamadas);
        Assert.Empty(hasheador.Contrasenas);
        Assert.Empty(tokens.TokensHasheados);
    }

    [Fact]
    public async Task Hashea_contrasena_antes_de_llamar_al_repositorio_para_activar()
    {
        repositorio.AlActivar = () => Assert.Equal(Entrada.NuevaContrasena, Assert.Single(hasheador.Contrasenas));
        hasheador.AlHashear = () => Assert.Empty(repositorio.Activaciones);

        await CrearCaso().EjecutarAsync(Entrada);

        var activacion = Assert.Single(repositorio.Activaciones);
        Assert.Equal("hash-password-prueba", activacion.PasswordHash);
        Assert.NotEqual(Entrada.NuevaContrasena, activacion.PasswordHash);
    }

    [Fact]
    public async Task Rechaza_reutilizacion_si_repositorio_rechaza_activar_aunque_validacion_previa_sea_exitosa()
    {
        var caso = CrearCaso();
        Assert.True((await caso.EjecutarAsync(Entrada)).Activada);
        repositorio.ResultadoActivacion = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(Entrada));

        Assert.Equal(2, repositorio.Validaciones.Count);
        Assert.Equal(2, repositorio.Activaciones.Count);
        Assert.All(repositorio.Activaciones, activacion => Assert.Equal("hash-de:token-raw", activacion.TokenHash));
    }
}
