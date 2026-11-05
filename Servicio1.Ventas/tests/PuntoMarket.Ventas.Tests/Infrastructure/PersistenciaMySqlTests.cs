using System.Data;
using PuntoMarket.Ventas.Infrastructure.Database;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Tests.Infrastructure;

public class PersistenciaMySqlTests
{
    private static DatabaseConnection Fabrica() => new("Server=localhost;Database=puntomarket_ventas_test_unit;");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Server=localhost;")]
    public void La_fabrica_rechaza_configuracion_sin_base(string? cadena)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DatabaseConnection(cadena!));
    }

    [Fact]
    public void La_fabrica_crea_conexiones_distintas_y_cerradas()
    {
        var fabrica = Fabrica();
        using var primera = fabrica.CreateConnection();
        using var segunda = fabrica.CreateConnection();

        Assert.NotSame(primera, segunda);
        Assert.Equal(ConnectionState.Closed, primera.State);
        Assert.Equal(ConnectionState.Closed, segunda.State);
    }

    [Fact]
    public async Task Sin_transaccion_confirmar_y_revertir_no_abren_conexion()
    {
        await using var unidad = new MySqlUnidadDeTrabajo(Fabrica());
        await unidad.ConfirmarAsync();
        await unidad.RevertirAsync();
        Assert.False(unidad.HayTransaccionActiva);
    }

    [Fact]
    public async Task Las_operaciones_que_escriben_o_bloquean_rechazan_autocommit()
    {
        await using var sesion = new PersistenciaVentasMySql(Fabrica());
        Func<Task>[] operaciones =
        [
            () => sesion.Ventas.InsertarCabeceraAsync(1, 1),
            () => sesion.Ventas.InsertarDetalleAsync(1, 1, 1, 1, 1m),
            () => sesion.Ventas.CerrarVentaAsync(1, "V-1", 1m),
            () => sesion.Ventas.ObtenerParaAnularAsync(1),
            () => sesion.Ventas.MarcarAnuladaAsync(1, "Error", 1),
            () => sesion.Precios.ObtenerPrecioVigenteAsync(1),
            () => sesion.Lotes.ObtenerVigentesParaDescuentoAsync(1),
            () => sesion.Lotes.DescontarAsync(1, 1),
            () => sesion.Lotes.RestaurarAsync(1, 1),
            () => sesion.Lotes.ObtenerVencidosConStockAsync(new DateOnly(2026, 6, 15)),
            () => sesion.Lotes.DesactivarAsync(1, 1)
        ];
        foreach (var operacion in operaciones)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(operacion);
            Assert.Contains("transacción activa", error.Message);
        }
    }

    [Fact]
    public async Task Disponer_es_idempotente_y_rechaza_reutilizar_la_unidad()
    {
        var unidad = new MySqlUnidadDeTrabajo(Fabrica());
        await unidad.DisposeAsync();
        await unidad.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => unidad.IniciarAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => unidad.ConfirmarAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => unidad.RevertirAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => unidad.AlquilarAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Una_lectura_cancelada_no_intenta_abrir_conexion()
    {
        await using var sesion = new PersistenciaVentasMySql(Fabrica());
        using var cancelacion = new CancellationTokenSource();
        cancelacion.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sesion.Clientes.ExisteActivoAsync(1, cancelacion.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sesion.Consultas.ListarAsync(false, 50, cancelacion.Token));
    }

    [Fact]
    public void El_detector_no_confunde_textos_o_timeouts_con_deadlocks()
    {
        var detector = new DetectorDeInterbloqueoMySql();
        Assert.False(detector.EsInterbloqueo(new Exception("1213 deadlock")));
        Assert.False(detector.EsInterbloqueo(new Exception("externa", new TimeoutException())));
        Assert.False(detector.EsInterbloqueo(new OperationCanceledException()));
    }
}
