using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Prueba que la regla "si el precio cambia hay que justificarlo" la aplique el SERVICIO.
//
// Antes vivia en el PageModel de Productos, de modo que solo se cumplia si el cambio
// entraba por esa pagina: el servicio no la aplicaba y cualquier otra entrada (una API, o
// el VentaFacade de la Parte 3) guardaba el precio sin justificacion. Estas pruebas no
// pasan por ninguna pagina, asi que si la regla volviera al PageModel, fallarian.
public class ProductServiceEnMemoriaTests
{
    private static readonly LookupOption[] Catalogo = [new(1, "Bolsa 1L")];

    private static (IProductService servicio, IPriceHistoryRepository historial) Crear()
    {
        var historial = new InMemoryPriceHistoryRepository();
        var productos = new InMemoryProductRepository(historial, Catalogo, Catalogo, Catalogo);

        return (new ProductService(productos, historial), historial);
    }

    private static Product NuevoProducto() => new()
    {
        Nombre = "Leche Entera",
        IdEmpaque = 1,
        PrecioVenta = 10.0m,
        PrecioCosto = 8.0m,
        StockMinimo = 5,
        IdCategoria = 1,
        IdProveedor = 1
    };

    private static Product Editado(int id, decimal precioVenta, string? motivo)
    {
        var producto = NuevoProducto();
        producto.Id = id;
        producto.PrecioVenta = precioVenta;
        producto.MotivoCambio = motivo;

        return producto;
    }

    [Fact]
    public async Task Cambiar_el_precio_sin_justificacion_es_rechazado()
    {
        var (servicio, _) = Crear();
        var id = await servicio.CreateAsync(NuevoProducto());

        await Assert.ThrowsAsync<MotivoDeCambioInvalidoException>(
            () => servicio.UpdateAsync(Editado(id, 12.0m, motivo: null)));
    }

    [Fact]
    public async Task Cambiar_el_precio_con_justificacion_demasiado_corta_es_rechazado()
    {
        var (servicio, _) = Crear();
        var id = await servicio.CreateAsync(NuevoProducto());

        await Assert.ThrowsAsync<MotivoDeCambioInvalidoException>(
            () => servicio.UpdateAsync(Editado(id, 12.0m, motivo: "ab")));
    }

    [Fact]
    public async Task Cambiar_el_precio_con_justificacion_valida_registra_el_historial()
    {
        var (servicio, historial) = Crear();
        var id = await servicio.CreateAsync(NuevoProducto());

        Assert.True(await servicio.UpdateAsync(
            Editado(id, 12.0m, motivo: "  subida   del proveedor ")));

        var registros = await historial.GetPriceHistoryAsync();
        var registro = Assert.Single(registros);

        Assert.Equal(10.0m, registro.PrecioVentaAnterior);
        Assert.Equal(12.0m, registro.PrecioVentaNuevo);

        // El servicio normalizo el motivo antes de guardarlo.
        Assert.Equal("Subida del proveedor", registro.MotivoCambio);
    }

    [Fact]
    public async Task Editar_sin_tocar_los_precios_no_pide_justificacion_ni_registra_historial()
    {
        var (servicio, historial) = Crear();
        var id = await servicio.CreateAsync(NuevoProducto());

        var sinCambioDePrecio = Editado(id, 10.0m, motivo: null);
        sinCambioDePrecio.StockMinimo = 50;

        Assert.True(await servicio.UpdateAsync(sinCambioDePrecio));

        Assert.Empty(await historial.GetPriceHistoryAsync());

        // Sin cambio de precio el motivo no corresponde, aunque el usuario escriba algo.
        Assert.Null(sinCambioDePrecio.MotivoCambio);
    }

    [Fact]
    public async Task Editar_sin_tocar_los_precios_descarta_el_motivo_que_llegue()
    {
        var (servicio, historial) = Crear();
        var id = await servicio.CreateAsync(NuevoProducto());

        var conMotivoSobrante = Editado(id, 10.0m, motivo: "motivo que no corresponde");

        Assert.True(await servicio.UpdateAsync(conMotivoSobrante));

        Assert.Null(conMotivoSobrante.MotivoCambio);
        Assert.Empty(await historial.GetPriceHistoryAsync());
    }

    [Fact]
    public async Task Actualizar_un_producto_que_no_existe_devuelve_false()
    {
        var (servicio, _) = Crear();

        Assert.False(await servicio.UpdateAsync(Editado(999, 10.0m, motivo: null)));
    }
}
