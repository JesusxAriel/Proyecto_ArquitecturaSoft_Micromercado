using System.Reflection;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

[Trait("Categoria", "MySql")]
public class StockActualMySqlTests
{
    private static readonly string? Conexion =
        Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL");

    private sealed record Pila(IFachadaVenta Venta, IFachadaAnulacion Anulacion);

    private static Pila CrearPila()
    {
        var baseDeDatos = (DatabaseConnection)RuntimeHelpers.GetUninitializedObject(typeof(DatabaseConnection));
        typeof(DatabaseConnection)
            .GetField("<ConnectionString>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(baseDeDatos, Conexion);

        var unidad = new MySqlUnidadDeTrabajo(baseDeDatos);
        var usuario = new UsuarioActualEnMemoria();
        var ventas = new MySqlVentaRepository(unidad);
        var lotes = new MySqlLoteRepository(unidad);
        var productos = new MySqlProductRepository(unidad, new MySqlPriceHistoryRepository(unidad), usuario);
        var stock = new FachadaActualizacionStockProducto(lotes, TimeProvider.System, unidad, usuario);

        return new Pila(
            new FachadaVenta(unidad, ventas, productos, stock, usuario, TimeProvider.System),
            new FachadaAnulacion(unidad, ventas, stock, usuario));
    }

    private static async Task<T> Escalar<T>(string sql)
    {
        await using var conexion = new MySqlConnection(Conexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        return (T)Convert.ChangeType(await comando.ExecuteScalarAsync() ?? 0, typeof(T));
    }

    private static Task<int> StockDe(int idProducto) =>
        Escalar<int>($"SELECT stockActual FROM PRODUCTO WHERE id = {idProducto}");

    private static Task<int> ProductosDesincronizados() =>
        Escalar<int>("""
            SELECT COUNT(*) FROM PRODUCTO p
            WHERE p.stockActual <> COALESCE(
                (SELECT SUM(l.cantidadDisponible) FROM LOTE l
                 WHERE l.idProducto = p.id AND l.estaActivo = 1), 0)
            """);

    private static Task<int> TotalDeVentas() => Escalar<int>("SELECT COUNT(*) FROM VENTA");

    [Fact]
    public async Task Vender_fallar_a_mitad_y_anular_mantienen_stockActual_igual_a_la_suma_de_lotes()
    {
        if (string.IsNullOrWhiteSpace(Conexion)) return;

        var pila = CrearPila();
        Assert.Equal(0, await ProductosDesincronizados());
        var stockInicial = await StockDe(1);
        var cantidad = Math.Min(stockInicial, 75);

        var resultado = await pila.Venta.RegistrarVentaAsync(
            new NuevaVentaDto(1, 100000m, [new LineaVentaDto(1, cantidad)]));

        Assert.Equal(stockInicial - cantidad, await StockDe(1));
        Assert.Equal(0, await ProductosDesincronizados());

        var ventasAntes = await TotalDeVentas();
        var stockAntes = await StockDe(1);
        await Assert.ThrowsAsync<StockInsuficienteException>(() => pila.Venta.RegistrarVentaAsync(
            new NuevaVentaDto(1, 100000m, [new LineaVentaDto(1, 1), new LineaVentaDto(2, 1000000)])));

        Assert.Equal(stockAntes, await StockDe(1));
        Assert.Equal(ventasAntes, await TotalDeVentas());
        Assert.Equal(0, await ProductosDesincronizados());

        await pila.Anulacion.AnularVentaAsync(resultado.IdVenta, "Prueba de stockActual");

        Assert.Equal(stockInicial, await StockDe(1));
        Assert.Equal(0, await ProductosDesincronizados());
    }

    [Fact]
    public async Task Ventas_concurrentes_del_mismo_producto_no_provocan_bloqueos_mutuos()
    {
        if (string.IsNullOrWhiteSpace(Conexion)) return;

        var stockInicial = await StockDe(1);
        var ids = new List<int>();

        for (var ronda = 0; ronda < 15; ronda++)
        {
            var pilas = new[] { CrearPila(), CrearPila(), CrearPila() };
            var resultados = await Task.WhenAll(pilas.Select(pila => pila.Venta.RegistrarVentaAsync(
                new NuevaVentaDto(1, 100000m, [new LineaVentaDto(1, 1)]))));
            ids.AddRange(resultados.Select(r => r.IdVenta));
        }

        Assert.Equal(stockInicial - ids.Count, await StockDe(1));
        Assert.Equal(0, await ProductosDesincronizados());

        var anulador = CrearPila();
        foreach (var id in ids)
        {
            await anulador.Anulacion.AnularVentaAsync(id, "Limpieza de la prueba concurrente");
        }

        Assert.Equal(stockInicial, await StockDe(1));
        Assert.Equal(0, await ProductosDesincronizados());
    }
}
