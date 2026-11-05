using System.ComponentModel.DataAnnotations;
using PuntoMarket.Ventas.Application.Customers;

namespace PuntoMarket.Ventas.Tests.Application;

public sealed class ClienteCreacionTests
{
    [Fact]
    public async Task Crea_cliente_normalizado_con_el_usuario_autenticado_y_confirma()
    {
        var unidad = new UnidadDeTrabajoEspia();
        var repositorio = new RepositorioClienteDoble();
        var servicio = new ClienteCreacionService(repositorio, unidad, new UsuarioActualEnMemoria(23, "cajero", "Cajero"));

        var resultado = await servicio.CrearAsync(new ClienteCreacionDto
        {
            NitCi = " 12345 ",
            RazonSocial = "  Cliente Nuevo "
        });

        Assert.Equal(17, resultado.Id);
        Assert.Equal("12345", repositorio.NitCi);
        Assert.Equal("Cliente Nuevo", resultado.RazonSocial);
        Assert.Equal(23, repositorio.IdUsuario);
        Assert.Equal(1, unidad.Iniciadas);
        Assert.Equal(1, unidad.Confirmadas);
        Assert.Equal(0, unidad.Revertidas);
    }

    [Fact]
    public async Task Rechaza_datos_invalidos_sin_abrir_transaccion()
    {
        var unidad = new UnidadDeTrabajoEspia();
        var servicio = new ClienteCreacionService(new RepositorioClienteDoble(), unidad, new UsuarioActualEnMemoria());

        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearAsync(new ClienteCreacionDto { NitCi = " ", RazonSocial = "" }));

        Assert.Equal(0, unidad.Iniciadas);
    }

    [Fact]
    public async Task Rechaza_duplicado_y_revierte_la_transaccion()
    {
        var unidad = new UnidadDeTrabajoEspia();
        var repositorio = new RepositorioClienteDoble { Fallo = new ClienteDuplicadoException() };
        var servicio = new ClienteCreacionService(repositorio, unidad, new UsuarioActualEnMemoria());

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => servicio.CrearAsync(
            new ClienteCreacionDto { NitCi = "12345", RazonSocial = "Cliente" }));

        Assert.Equal(1, unidad.Revertidas);
        Assert.Equal(0, unidad.Confirmadas);
    }

    private sealed class RepositorioClienteDoble : IClienteCreacionRepository
    {
        public string? NitCi { get; private set; }
        public int IdUsuario { get; private set; }
        public Exception? Fallo { get; init; }

        public Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion, CancellationToken cancellationToken = default)
        {
            if (Fallo is not null) throw Fallo;
            NitCi = nitCi;
            IdUsuario = idUsuarioCreacion;
            return Task.FromResult(17);
        }
    }
}
