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

    [Theory]
    [InlineData("  leche  ", true)]
    [InlineData("bolsa", true)]
    [InlineData("lacteos", false)]
    [InlineData("distribuidora", false)]
    public async Task Busqueda_de_sales_solo_incluye_nombre_y_presentacion(string termino, bool coincide)
    {
        var historial = new InMemoryPriceHistoryRepository();
        var repositorio = new InMemoryProductRepository(historial,
            Catalogo, [new(1, "Lacteos")], [new(1, "Distribuidora")]);
        IProductService servicio = new ProductService(repositorio, historial);
        var id = await servicio.CreateAsync(NuevoProducto());

        var sales = await servicio.GetPagedForSalesAsync(1, 10, termino);
        var general = await servicio.GetPagedAsync(1, 10, termino);

        Assert.Equal(coincide ? 1 : 0, sales.TotalCount);
        Assert.Equal(id, Assert.Single(general.Items).Id);
        if (coincide)
        {
            var producto = Assert.Single(sales.Items);
            Assert.Equal(id, producto.Id);
            Assert.Equal("Bolsa 1L", producto.EmpaquePresentacion);
            Assert.Equal(10.0m, producto.PrecioVenta);
            Assert.Equal(0, producto.StockCalculado);
        }
    }

    [Fact]
    public async Task Busqueda_de_sales_conserva_limite_y_total_de_coincidencias()
    {
        var (servicio, _) = Crear();
        for (var i = 0; i < 12; i++)
        {
            var producto = NuevoProducto();
            producto.Nombre = $"Leche {i:D2}";
            await servicio.CreateAsync(producto);
        }

        var resultado = await servicio.GetPagedForSalesAsync(1, 10, "leche");

        Assert.Equal(12, resultado.TotalCount);
        Assert.Equal(10, resultado.Items.Count);
    }

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

    [Fact]
    public async Task Historial_por_producto_no_incluye_cambios_de_otros_productos_y_conserva_orden()
    {
        var historial = new InMemoryPriceHistoryRepository();
        var servicio = new ProductService(
            new InMemoryProductRepository(historial, Catalogo, Catalogo, Catalogo),
            historial);
        var producto = NuevoProducto();
        var idProducto = await servicio.CreateAsync(producto);
        var fechaAntigua = new DateTime(2025, 1, 1, 10, 0, 0);
        var fechaReciente = fechaAntigua.AddDays(1);

        await historial.AddPriceHistoryAsync(new ProductPriceHistory
        {
            IdProducto = idProducto,
            NombreProducto = producto.Nombre,
            PrecioVentaAnterior = 10m,
            PrecioVentaNuevo = 11m,
            FechaCambio = fechaAntigua
        });
        await historial.AddPriceHistoryAsync(new ProductPriceHistory
        {
            IdProducto = 999,
            NombreProducto = "Otro producto",
            PrecioVentaAnterior = 20m,
            PrecioVentaNuevo = 21m,
            FechaCambio = fechaReciente
        });
        await historial.AddPriceHistoryAsync(new ProductPriceHistory
        {
            IdProducto = idProducto,
            NombreProducto = producto.Nombre,
            PrecioVentaAnterior = 11m,
            PrecioVentaNuevo = 12m,
            FechaCambio = fechaReciente
        });

        var registros = await servicio.GetPriceHistoryByProductIdAsync(idProducto);

        Assert.Equal(2, registros.Count);
        Assert.All(registros, registro => Assert.Equal(idProducto, registro.IdProducto));
        Assert.Equal(12m, registros[0].PrecioVentaNuevo);
        Assert.Equal(11m, registros[1].PrecioVentaNuevo);
    }

    [Fact]
    public async Task Historial_por_producto_rechaza_identificador_invalido()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servicio.GetPriceHistoryByProductIdAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servicio.GetPriceHistoryByProductIdAsync(-1));
    }

    [Fact]
    public async Task Historial_por_id_devuelve_el_registro_exacto_y_no_modifica_su_contenido()
    {
        var (servicio, historial) = Crear();
        var primero = new ProductPriceHistory
        {
            IdProducto = 1,
            NombreProducto = "Producto A",
            PrecioVentaAnterior = 10m,
            PrecioVentaNuevo = 12m,
            MotivoCambio = "Ajuste autorizado",
            IdUsuario = 7,
            FechaCambio = new DateTime(2025, 2, 3, 12, 30, 0)
        };
        var segundo = new ProductPriceHistory
        {
            IdProducto = 2,
            NombreProducto = "Producto B",
            PrecioVentaAnterior = 20m,
            PrecioVentaNuevo = 22m,
            MotivoCambio = "Actualización de costo",
            IdUsuario = 8,
            FechaCambio = new DateTime(2025, 2, 4, 12, 30, 0)
        };

        await historial.AddPriceHistoryAsync(primero);
        await historial.AddPriceHistoryAsync(segundo);

        var registros = await servicio.GetPriceHistoryAsync();
        var registroSegundo = Assert.Single(registros.Where(registro => registro.NombreProducto == "Producto B"));
        var encontrado = await servicio.GetPriceHistoryByIdAsync(registroSegundo.Id);
        var inexistente = await servicio.GetPriceHistoryByIdAsync(999);

        Assert.NotNull(encontrado);
        Assert.Equal(registroSegundo.Id, encontrado.Id);
        Assert.Equal(2, encontrado.IdProducto);
        Assert.Equal("Producto B", encontrado.NombreProducto);
        Assert.Equal(20m, encontrado.PrecioVentaAnterior);
        Assert.Equal(22m, encontrado.PrecioVentaNuevo);
        Assert.Equal("Actualización de costo", encontrado.MotivoCambio);
        Assert.Equal(8, encontrado.IdUsuario);
        Assert.Equal(segundo.FechaCambio, encontrado.FechaCambio);
        Assert.Null(inexistente);
    }

    [Fact]
    public async Task Historial_por_id_rechaza_identificador_no_positivo()
    {
        var (servicio, _) = Crear();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servicio.GetPriceHistoryByIdAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => servicio.GetPriceHistoryByIdAsync(-1));
    }
}
