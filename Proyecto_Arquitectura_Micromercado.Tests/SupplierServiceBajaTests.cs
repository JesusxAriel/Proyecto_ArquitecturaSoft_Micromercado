using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Prueba que la regla "no se da de baja un proveedor con productos" la aplique el
// SERVICIO. Antes vivia en el PageModel de Proveedores, asi que cualquier otra entrada al
// sistema se la saltaba. Estas pruebas no pasan por ninguna pagina.
public class SupplierServiceBajaTests
{
    private static readonly LookupOption[] Catalogo = [new(1, "Catalogo")];

    private readonly InMemoryProductRepository productos;
    private readonly SupplierService servicio;

    public SupplierServiceBajaTests()
    {
        productos = new InMemoryProductRepository(
            new InMemoryPriceHistoryRepository(),
            Catalogo,
            Catalogo,
            Catalogo);

        servicio = new SupplierService(new InMemorySupplierRepository(), productos);
    }

    private Task<int> NuevoProveedor(string nombre) => servicio.CreateAsync(new Supplier
    {
        NombreEmpresa = nombre,
        NumeroEmpresa = "70123456",
        CorreoReferencia = "contacto@empresa.com.bo",
        EsAutogestionado = false
    });

    private Task<int> NuevoProducto(int idProveedor, string nombre) => productos.CreateAsync(new Product
    {
        Nombre = nombre,
        IdEmpaque = 1,
        PrecioVenta = 10.0m,
        PrecioCosto = 8.0m,
        StockMinimo = 5,
        IdCategoria = 1,
        IdProveedor = idProveedor
    });

    [Fact]
    public async Task Un_proveedor_con_productos_activos_no_se_puede_dar_de_baja()
    {
        var idProveedor = await NuevoProveedor("Distribuidora Andina");
        await NuevoProducto(idProveedor, "Leche Entera");

        var ex = await Assert.ThrowsAsync<SupplierConProductosException>(
            () => servicio.SoftDeleteAsync(idProveedor));

        Assert.Equal(SupplierConProductosException.Mensaje, ex.Message);
        Assert.NotNull(await servicio.GetByIdAsync(idProveedor));
    }

    [Fact]
    public async Task Un_proveedor_sin_productos_se_da_de_baja()
    {
        var idProveedor = await NuevoProveedor("Distribuidora Andina");

        Assert.True(await servicio.SoftDeleteAsync(idProveedor));
        Assert.Null(await servicio.GetByIdAsync(idProveedor));
    }

    [Fact]
    public async Task Los_productos_de_otro_proveedor_no_bloquean_la_baja()
    {
        var conProductos = await NuevoProveedor("Distribuidora Andina");
        var sinProductos = await NuevoProveedor("Embotelladora del Sur");
        await NuevoProducto(conProductos, "Leche Entera");

        Assert.True(await servicio.SoftDeleteAsync(sinProductos));
    }

    [Fact]
    public async Task Cuando_sus_productos_se_dan_de_baja_el_proveedor_ya_se_puede_eliminar()
    {
        var idProveedor = await NuevoProveedor("Distribuidora Andina");
        var idProducto = await NuevoProducto(idProveedor, "Leche Entera");
        await productos.SoftDeleteAsync(idProducto);

        Assert.True(await servicio.SoftDeleteAsync(idProveedor));
    }

    [Fact]
    public async Task Un_identificador_no_positivo_es_rechazado_antes_de_consultar()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => servicio.SoftDeleteAsync(0));
    }
}
