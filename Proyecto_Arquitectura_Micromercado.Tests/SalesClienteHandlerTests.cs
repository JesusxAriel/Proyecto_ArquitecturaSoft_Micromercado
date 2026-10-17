using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
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

        var resultado = await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "  123456  ", RazonSocial = "  Ana Pérez  " }, default);
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

        var resultado = await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "Ana" }, default);
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

        var resultado = await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "  " }, default);
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

        var resultado = await pagina.OnPostCrearClienteAsync(new ClienteCreacionDto(), default);
        var json = JsonSerializer.SerializeToElement(resultado.Value);

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Ingrese el CI/NIT. Ingrese la razón social.", json.GetProperty("mensaje").GetString());
        Assert.Equal(0, servicio.Llamadas);
    }

    [Fact]
    public async Task Un_cuerpo_nulo_devuelve_400()
    {
        var pagina = CrearPagina(new CreacionPrueba());

        var resultado = await pagina.OnPostCrearClienteAsync(null, default);

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task Un_error_de_persistencia_devuelve_503_sin_detalles_internos()
    {
        var servicio = new CreacionPrueba { Error = new ErrorDePersistenciaException(new Exception("Información interna")) };
        var pagina = CrearPagina(servicio);

        var resultado = await pagina.OnPostCrearClienteAsync(
            new ClienteCreacionDto { NitCi = "123", RazonSocial = "Ana" }, default);

        Assert.Equal(503, resultado.StatusCode);
        Assert.DoesNotContain("Información interna", JsonSerializer.Serialize(resultado.Value));
    }

    private static IndexModel CrearPagina(CreacionPrueba servicio) =>
        new(null!, null!, null!, null!, null!, servicio, NullLogger<IndexModel>.Instance);

    private sealed class CreacionPrueba : IClienteCreacionService
    {
        public int Llamadas { get; private set; }
        public int IdNuevo { get; init; } = 1;
        public Exception? Error { get; init; }

        public Task<int> CrearAsync(ClienteCreacionDto cliente, CancellationToken cancellationToken = default)
        {
            Llamadas++;
            return Error is null ? Task.FromResult(IdNuevo) : Task.FromException<int>(Error);
        }
    }
}
