using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class UnidadDeTrabajoTests
{
    [Fact]
    public async Task Iniciar_abre_la_transaccion()
    {
        var unidad = new UnidadDeTrabajoEnMemoria();

        await unidad.IniciarAsync();

        Assert.True(unidad.HayTransaccionActiva);
    }

    [Fact]
    public async Task Iniciar_dos_veces_sin_cerrar_es_un_error_de_orquestacion()
    {
        var unidad = new UnidadDeTrabajoEnMemoria();
        await unidad.IniciarAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unidad.IniciarAsync());
    }

    [Fact]
    public async Task Confirmar_cierra_la_transaccion()
    {
        var unidad = new UnidadDeTrabajoEnMemoria();
        await unidad.IniciarAsync();

        await unidad.ConfirmarAsync();

        Assert.False(unidad.HayTransaccionActiva);
        Assert.Equal(1, unidad.ConfirmacionesRealizadas);

        await unidad.IniciarAsync();
        Assert.True(unidad.HayTransaccionActiva);
    }

    [Fact]
    public async Task Revertir_cierra_la_transaccion()
    {
        var unidad = new UnidadDeTrabajoEnMemoria();
        await unidad.IniciarAsync();

        await unidad.RevertirAsync();

        Assert.False(unidad.HayTransaccionActiva);
        Assert.Equal(1, unidad.ReversionesRealizadas);
    }

    [Fact]
    public async Task Confirmar_o_revertir_sin_transaccion_no_hace_nada()
    {
        var unidad = new UnidadDeTrabajoEnMemoria();

        await unidad.ConfirmarAsync();
        await unidad.RevertirAsync();

        Assert.False(unidad.HayTransaccionActiva);
        Assert.Equal(0, unidad.ConfirmacionesRealizadas);
        Assert.Equal(0, unidad.ReversionesRealizadas);
    }

    [Fact]
    public async Task Se_usa_a_traves_del_puerto_sin_conocer_el_motor()
    {
        IUnidadDeTrabajo unidad = new UnidadDeTrabajoEnMemoria();

        await unidad.IniciarAsync();
        await unidad.ConfirmarAsync();

        Assert.Equal(1, ((UnidadDeTrabajoEnMemoria)unidad).ConfirmacionesRealizadas);
    }

    [Fact]
    public async Task El_puerto_del_historial_ya_no_pide_conexion_ni_transaccion()
    {
        CreatorPriceHistoryRepository creador = new CreatorPriceHistoryRepositoryEnMemoria();
        IPriceHistoryRepository historial = creador.ObtenerRepositorio();

        await historial.AddPriceHistoryAsync(new ProductPriceHistory
        {
            IdProducto = 1,
            PrecioVentaAnterior = 10m,
            PrecioVentaNuevo = 12m,
            MotivoCambio = "Prueba",
            IdUsuario = 1
        });

        Assert.Single(await historial.GetPriceHistoryAsync());
    }
}
