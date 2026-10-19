using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Facades;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Background;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

[Trait("Categoria", "MySql")]
public class StockActualMySqlTests
{
    private static readonly string? Conexion =
        Environment.GetEnvironmentVariable("MICROMERCADO_PRUEBAS_MYSQL");

    private sealed record Pila(IFachadaVenta Venta, IFachadaAnulacion Anulacion, IFachadaActualizacionStockProducto Stock);

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
            new FachadaVenta(unidad, ventas, productos, stock, usuario, TimeProvider.System, new DetectorDeInterbloqueoMySql(), new MySqlClienteConsultaRepository(unidad)),
            new FachadaAnulacion(unidad, ventas, stock, usuario),
            stock);
    }

    private static async Task<T> Escalar<T>(string sql)
    {
        await using var conexion = new MySqlConnection(Conexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        return (T)Convert.ChangeType(await comando.ExecuteScalarAsync() ?? 0, typeof(T));
    }

    private static async Task Ejecutar(string sql)
    {
        await using var conexion = new MySqlConnection(Conexion);
        await conexion.OpenAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
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
            new NuevaVentaDto(1, [new LineaVentaDto(1, cantidad)]));

        Assert.Equal(stockInicial - cantidad, await StockDe(1));
        Assert.Equal(0, await ProductosDesincronizados());

        var ventasAntes = await TotalDeVentas();
        var stockAntes = await StockDe(1);
        await Assert.ThrowsAsync<StockInsuficienteException>(() => pila.Venta.RegistrarVentaAsync(
            new NuevaVentaDto(1, [new LineaVentaDto(1, 1), new LineaVentaDto(2, 1000000)])));

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
                new NuevaVentaDto(1, [new LineaVentaDto(1, 1)]))));
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

    [Fact]
    public async Task Dar_de_baja_lotes_vencidos_deja_stockActual_igual_a_la_suma_de_lotes_activos()
    {
        if (string.IsNullOrWhiteSpace(Conexion)) return;

        var pila = CrearPila();
        var idLote = await Escalar<int>(
            "SELECT id FROM LOTE WHERE estaActivo = 1 AND cantidadDisponible > 0 AND fechaVencimiento IS NOT NULL ORDER BY id LIMIT 1");
        var idProducto = await Escalar<int>($"SELECT idProducto FROM LOTE WHERE id = {idLote}");
        var cantidadDelLote = await Escalar<int>($"SELECT cantidadDisponible FROM LOTE WHERE id = {idLote}");
        var fechaOriginal = await Escalar<string>($"SELECT DATE_FORMAT(fechaVencimiento, '%Y-%m-%d') FROM LOTE WHERE id = {idLote}");
        var stockAntes = await StockDe(idProducto);

        await Ejecutar($"UPDATE LOTE SET fechaVencimiento = DATE_SUB(CURDATE(), INTERVAL 2 DAY) WHERE id = {idLote}");
        try
        {
            var resultado = await pila.Stock.DarDeBajaLotesVencidosAsync();

            Assert.True(resultado.Lotes >= 1);
            Assert.True(resultado.Unidades >= cantidadDelLote);
            Assert.Equal(0, await Escalar<int>($"SELECT estaActivo FROM LOTE WHERE id = {idLote}"));
            Assert.Equal(1, await Escalar<int>($"SELECT idUsuarioActualizacion FROM LOTE WHERE id = {idLote}"));
            Assert.Equal(stockAntes - cantidadDelLote, await StockDe(idProducto));
            Assert.Equal(0, await ProductosDesincronizados());
        }
        finally
        {
            await Ejecutar($"UPDATE LOTE SET estaActivo = 1, fechaVencimiento = '{fechaOriginal}' WHERE id = {idLote}");
        }

        Assert.Equal(stockAntes, await StockDe(idProducto));
        Assert.Equal(0, await ProductosDesincronizados());
    }

    [Fact]
    public async Task La_baja_automatica_deja_stockActual_igual_a_la_suma_de_los_lotes_activos()
    {
        if (string.IsNullOrWhiteSpace(Conexion)) return;

        var pila = CrearPila();
        var idLote = await Escalar<int>(
            "SELECT id FROM LOTE WHERE estaActivo = 1 AND cantidadDisponible > 0 AND fechaVencimiento IS NOT NULL ORDER BY id LIMIT 1");
        var idProducto = await Escalar<int>($"SELECT idProducto FROM LOTE WHERE id = {idLote}");
        var cantidadDelLote = await Escalar<int>($"SELECT cantidadDisponible FROM LOTE WHERE id = {idLote}");
        var fechaOriginal = await Escalar<string>($"SELECT DATE_FORMAT(fechaVencimiento, '%Y-%m-%d') FROM LOTE WHERE id = {idLote}");
        var usuarioOriginal = await Escalar<string>($"SELECT COALESCE(CAST(idUsuarioActualizacion AS CHAR), 'NULL') FROM LOTE WHERE id = {idLote}");
        var stockAntes = await StockDe(idProducto);
        var proveedor = new ServiceCollection()
            .AddScoped<IBajaDeLotesService>(_ => new BajaDeLotesService(pila.Stock))
            .BuildServiceProvider();
        var tarea = new BajaAutomaticaDeLotesVencidos(
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<BajaAutomaticaDeLotesVencidos>.Instance);

        await Ejecutar($"UPDATE LOTE SET fechaVencimiento = DATE_SUB(CURDATE(), INTERVAL 2 DAY) WHERE id = {idLote}");
        try
        {
            await tarea.EjecutarUnaVezAsync(CancellationToken.None);

            Assert.Equal(0, await Escalar<int>($"SELECT estaActivo FROM LOTE WHERE id = {idLote}"));
            Assert.Equal(1, await Escalar<int>($"SELECT idUsuarioActualizacion IS NULL FROM LOTE WHERE id = {idLote}"));
            Assert.Equal(stockAntes - cantidadDelLote, await StockDe(idProducto));
            Assert.Equal(0, await ProductosDesincronizados());
        }
        finally
        {
            await Ejecutar($"UPDATE LOTE SET estaActivo = 1, fechaVencimiento = '{fechaOriginal}', idUsuarioActualizacion = {usuarioOriginal} WHERE id = {idLote}");
        }

        Assert.Equal(stockAntes, await StockDe(idProducto));
        Assert.Equal(0, await ProductosDesincronizados());
    }

    [Fact]
    public async Task La_tabla_VENTA_no_tiene_columnas_de_efectivo_ni_cambio_y_registrar_una_venta_funciona()
    {
        if (string.IsNullOrWhiteSpace(Conexion)) return;

        var pila = CrearPila();
        var resultado = await pila.Venta.RegistrarVentaAsync(new NuevaVentaDto(1, [new LineaVentaDto(1, 1)]));
        try
        {
            Assert.Equal(0, await Escalar<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'VENTA' AND COLUMN_NAME IN ('efectivoPagado', 'cambio')"));
            Assert.Equal(1, await Escalar<int>($"SELECT montoTotal = {resultado.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)} FROM VENTA WHERE id = {resultado.IdVenta}"));
        }
        finally
        {
            await pila.Anulacion.AnularVentaAsync(resultado.IdVenta, "Prueba automatizada");
        }
    }
}
