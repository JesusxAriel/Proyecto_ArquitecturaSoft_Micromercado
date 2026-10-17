using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Pages.Sales;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SalesClienteHandlerTests
{
    [Fact]
    public async Task El_cliente_creado_se_devuelve_con_su_id_y_sus_datos_normalizados()
    {
        var servicio = new CreacionPrueba { IdNuevo = 41 };
        var pagina = CrearPagina(servicio);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "  123456  ", RazonSocial = "  Ana Pérez  " }, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.True(json.GetProperty("exito").GetBoolean());
        Assert.Equal(41, json.GetProperty("id").GetInt32());
        Assert.Equal("123456", json.GetProperty("nitCi").GetString());
        Assert.Equal("Ana Pérez", json.GetProperty("razonSocial").GetString());
        Assert.Equal(1, servicio.Llamadas);
    }

    [Fact]
    public async Task Un_cliente_duplicado_devuelve_400_con_el_mensaje_del_dominio()
    {
        var servicio = new CreacionPrueba { Error = new ClienteDuplicadoException() };
        var pagina = CrearPagina(servicio);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "Ana" }, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal(new ClienteDuplicadoException().Message, json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Los_datos_invalidos_del_servicio_devuelven_400_con_su_mensaje()
    {
        var servicio = new CreacionPrueba { Error = new ValidationException("Ingrese la razón social.") };
        var pagina = CrearPagina(servicio);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "  " }, default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Ingrese la razón social.", json.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Un_binding_invalido_devuelve_400_con_los_mensajes_y_no_llama_al_servicio()
    {
        var servicio = new CreacionPrueba();
        var pagina = CrearPagina(servicio);
        pagina.ModelState.AddModelError("NitCi", "Ingrese el CI/NIT.");
        pagina.ModelState.AddModelError("RazonSocial", "Ingrese la razón social.");

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(new ClienteCreacionDto(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Ingrese el CI/NIT. Ingrese la razón social.", json.GetProperty("mensaje").GetString());
        Assert.Equal(0, servicio.Llamadas);
    }

    [Fact]
    public async Task Un_cuerpo_nulo_devuelve_400()
    {
        var pagina = CrearPagina(new CreacionPrueba());

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(null, default));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task Un_error_de_persistencia_devuelve_503_sin_detalles_internos()
    {
        var servicio = new CreacionPrueba { Error = new ErrorDePersistenciaException(new Exception("Información interna")) };
        var pagina = CrearPagina(servicio);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "Ana" }, default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
    }

    private static CancellationToken Cancelado()
    {
        var origen = new CancellationTokenSource();
        origen.Cancel();
        return origen.Token;
    }

    private static ClienteCreacionDto ClienteValido() => new() { NitCi = "123", RazonSocial = "Ana" };

    [Fact]
    public async Task Con_la_peticion_ya_cancelada_no_se_crea_el_cliente_y_no_se_devuelve_nada()
    {
        var servicio = new CreacionPrueba();
        var registro = new RegistroEspia();
        var pagina = CrearPagina(servicio, registro);

        var resultado = await pagina.OnPostCrearClienteAsync(ClienteValido(), Cancelado());

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(0, servicio.Llamadas);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_la_peticion_se_cancela_durante_la_creacion_no_hay_500_ni_logs()
    {
        var origen = new CancellationTokenSource();
        var servicio = new CreacionPrueba { Antes = () => origen.Cancel(), Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(servicio, registro);

        var resultado = await pagina.OnPostCrearClienteAsync(ClienteValido(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Empty(registro.Entradas);
    }

    [Fact]
    public async Task Si_el_cliente_se_va_mientras_se_crea_el_registro_igual_se_completa()
    {
        var origen = new CancellationTokenSource();
        var servicio = new CreacionPrueba { Antes = () => origen.Cancel() };
        var pagina = CrearPagina(servicio);

        var resultado = await pagina.OnPostCrearClienteAsync(ClienteValido(), origen.Token);

        Assert.IsType<EmptyResult>(resultado);
        Assert.Equal(1, servicio.Llamadas);
        Assert.False(servicio.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_token_de_la_peticion_no_llega_al_servicio_de_creacion()
    {
        var origen = new CancellationTokenSource();
        var servicio = new CreacionPrueba();
        var pagina = CrearPagina(servicio);

        await pagina.OnPostCrearClienteAsync(ClienteValido(), origen.Token);
        origen.Cancel();

        Assert.NotNull(servicio.TokenRecibido);
        Assert.NotEqual(origen.Token, servicio.TokenRecibido);
        Assert.False(servicio.TokenRecibido!.Value.IsCancellationRequested);
    }

    [Fact]
    public async Task El_vencimiento_del_tiempo_de_la_creacion_devuelve_503_con_json_valido_y_un_aviso()
    {
        var servicio = new CreacionPrueba { Error = new TaskCanceledException() };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(ClienteValido(), default));
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(503, resultado.StatusCode);
        Assert.False(json.GetProperty("exito").GetBoolean());
        Assert.Equal("No se pudo registrar el cliente. Intente nuevamente.", json.GetProperty("mensaje").GetString());
        Assert.Equal([LogLevel.Warning], registro.Niveles);
    }

    [Fact]
    public async Task Una_excepcion_inesperada_en_la_creacion_se_registra_como_error()
    {
        var servicio = new CreacionPrueba { Error = new NullReferenceException("Información interna") };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(ClienteValido(), default));

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
        Assert.Equal([LogLevel.Error], registro.Niveles);
    }

    [Fact]
    public async Task Un_duplicado_sigue_devolviendo_400_aunque_la_peticion_este_activa()
    {
        var servicio = new CreacionPrueba { Error = new ClienteDuplicadoException() };
        var registro = new RegistroEspia();
        var pagina = CrearPagina(servicio, registro);

        var resultado = Assert.IsType<JsonResult>(await pagina.OnPostCrearClienteAsync(ClienteValido(), default));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Empty(registro.Entradas);
    }

    private static IndexModel CrearPagina(CreacionPrueba servicio, RegistroEspia? registro = null) =>
        new(null!, null!, null!, null!, null!, servicio, registro ?? new RegistroEspia());

    private sealed class CreacionPrueba : IClienteCreacionService
    {
        public int Llamadas { get; private set; }
        public int IdNuevo { get; init; } = 1;
        public CancellationToken? TokenRecibido { get; private set; }
        public Exception? Error { get; init; }
        public Action? Antes { get; init; }

        public Task<int> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            TokenRecibido = cancellationToken;
            Antes?.Invoke();
            return Error is null ? Task.FromResult(IdNuevo) : Task.FromException<int>(Error);
        }
    }
}
