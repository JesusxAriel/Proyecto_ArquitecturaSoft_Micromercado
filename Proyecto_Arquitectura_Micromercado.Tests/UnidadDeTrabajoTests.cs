using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Verifica el contrato del puerto IUnidadDeTrabajo sobre el adaptador en memoria.
//
// El adaptador de MySQL no se puede probar aca sin una base de datos, por eso lo que se
// fija son las reglas del CONTRATO, que ambos motores deben cumplir igual: iniciar dos
// veces es un error, y confirmar o revertir sin transaccion abierta no hace nada. Si
// alguien cambia una de las dos implementaciones y rompe esa simetria, se rompe el LSP.
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

        // Las transacciones anidadas no estan soportadas. Silenciarlo esconderia un
        // error de quien orquesta, asi que los dos motores fallan igual.
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

        // Cerrada una, se puede abrir la siguiente.
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

        // Debe ser seguro llamarlos desde un catch sin saber si alcanzo a abrirse.
        await unidad.ConfirmarAsync();
        await unidad.RevertirAsync();

        Assert.False(unidad.HayTransaccionActiva);
        Assert.Equal(0, unidad.ConfirmacionesRealizadas);
        Assert.Equal(0, unidad.ReversionesRealizadas);
    }

    // El puerto se usa a traves de la abstraccion, no del tipo concreto: es lo que va a
    // hacer el VentaFacade de la Parte 3.
    [Fact]
    public async Task Se_usa_a_traves_del_puerto_sin_conocer_el_motor()
    {
        IUnidadDeTrabajo unidad = new UnidadDeTrabajoEnMemoria();

        await unidad.IniciarAsync();
        await unidad.ConfirmarAsync();

        Assert.Equal(1, ((UnidadDeTrabajoEnMemoria)unidad).ConfirmacionesRealizadas);
    }

    // El puerto del historial quedo sin rastros de ADO.NET: esta llamada compila con la
    // firma limpia, sin conexion ni transaccion.
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
