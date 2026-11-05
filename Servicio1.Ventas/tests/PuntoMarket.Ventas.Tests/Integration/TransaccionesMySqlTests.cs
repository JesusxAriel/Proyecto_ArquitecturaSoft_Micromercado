using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Sales;
using PuntoMarket.Ventas.Domain.Stock;
using PuntoMarket.Ventas.Infrastructure.Persistence;

namespace PuntoMarket.Ventas.Tests.Integration;

[CollectionDefinition("MySql aislado", DisableParallelization = true)]
public sealed class MySqlCollectionDefinition;

[Collection("MySql aislado")]
[Trait("Categoria", "MySqlIntegration")]
public class TransaccionesMySqlTests
{
    [MySqlIntegrationFact]
    public async Task Commit_persiste_venta_detalles_FEFO_stock_y_consultas()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        await using var sesion = new PersistenciaVentasMySql(db.Conexiones);

        var venta = await db.Registro(sesion, datos).RegistrarVentaAsync(
            new NuevaVentaDto(datos.Cliente, [new LineaVentaDto(datos.Producto, 5, 10m)]));

        Assert.False(sesion.Unidad.HayTransaccionActiva);
        Assert.Equal(50m, venta.Total);
        Assert.Equal(1, await db.CantidadVentas(datos.Cliente));
        Assert.Equal(0, await db.Disponible(datos.Lote1));
        Assert.Equal(8, await db.Disponible(datos.Lote2));
        Assert.Equal(8, await db.Stock(datos.Producto));
        var persistida = await sesion.Ventas.GetByIdAsync(venta.IdVenta);
        Assert.NotNull(persistida);
        Assert.Equal(datos.Usuario, persistida.IdUsuario);
        Assert.Equal([(datos.Lote1, 3), (datos.Lote2, 2)], persistida.Detalles.OrderBy(d => d.Id).Select(d => (d.IdLote, d.Cantidad)));
        var consultas = new VentaConsultaService(sesion.Ventas, sesion.Consultas);
        Assert.Contains(await consultas.ListarAsync(false), v => v.Id == venta.IdVenta);
        var detalle = await consultas.ObtenerDetalleAsync(venta.IdVenta);
        Assert.NotNull(detalle);
        Assert.Equal(5, Assert.Single(detalle.Lineas).Cantidad);
        Assert.Equal(2, detalle.Lineas[0].Lotes.Count);
        var totalSql = await db.EscalarAsync<decimal>(
            "SELECT COALESCE(SUM(montoTotal),0) FROM VENTA WHERE DATE(fechaVenta)=CURDATE() AND estado='COMPLETADA';");
        Assert.Equal(totalSql, await consultas.ObtenerVentasDelDiaAsync());
    }

    [MySqlIntegrationFact]
    public async Task Un_error_en_el_segundo_producto_revierte_cabecera_detalle_y_stock()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        await using var sesion = new PersistenciaVentasMySql(db.Conexiones);

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Registro(sesion, datos).RegistrarVentaAsync(
            new NuevaVentaDto(datos.Cliente,
                [new LineaVentaDto(datos.Producto, 5), new LineaVentaDto(int.MaxValue, 1)])));

        Assert.False(sesion.Unidad.HayTransaccionActiva);
        Assert.Equal(0, await db.CantidadVentas(datos.Cliente));
        Assert.Equal(0, await db.EscalarAsync<int>("SELECT COUNT(*) FROM DETALLE_VENTA WHERE idProducto=@id;", ("@id", datos.Producto)));
        Assert.Equal(3, await db.Disponible(datos.Lote1));
        Assert.Equal(10, await db.Disponible(datos.Lote2));
        Assert.Equal(13, await db.Stock(datos.Producto));
    }

    [MySqlIntegrationFact]
    public async Task Anulacion_restaura_lotes_y_una_segunda_anulacion_no_restaura_de_nuevo()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        await using var sesion = new PersistenciaVentasMySql(db.Conexiones);
        var venta = await db.Registro(sesion, datos).RegistrarVentaAsync(
            new NuevaVentaDto(datos.Cliente, [new LineaVentaDto(datos.Producto, 5)]));

        await db.Anulacion(sesion, datos).AnularVentaAsync(venta.IdVenta, "  Prueba  ");
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Anulacion(sesion, datos).AnularVentaAsync(venta.IdVenta, "Segunda"));

        Assert.Equal(3, await db.Disponible(datos.Lote1));
        Assert.Equal(10, await db.Disponible(datos.Lote2));
        Assert.Equal(13, await db.Stock(datos.Producto));
        var detalle = await sesion.Consultas.ObtenerDetalleAsync(venta.IdVenta);
        Assert.NotNull(detalle);
        Assert.Equal("ANULADA", detalle.Estado);
        Assert.Equal("Prueba", detalle.MotivoAnulacion);
        Assert.NotNull(detalle.FechaAnulacion);
        Assert.Equal("Usuario de prueba", detalle.UsuarioAnulacion);
        Assert.Equal(datos.Usuario, await db.EscalarAsync<int>("SELECT idUsuarioAnulacion FROM VENTA WHERE id=@id;", ("@id", venta.IdVenta)));
    }

    [MySqlIntegrationFact]
    public async Task Restauracion_que_excede_el_limite_revierte_tambien_el_lote_previamente_restaurado()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        await using var sesion = new PersistenciaVentasMySql(db.Conexiones);
        var venta = await db.Registro(sesion, datos).RegistrarVentaAsync(
            new NuevaVentaDto(datos.Cliente, [new LineaVentaDto(datos.Producto, 5)]));
        // Alteración válida solo de los datos exclusivos de este caso: fuerza el fallo del segundo lote.
        await db.EjecutarAsync("UPDATE LOTE SET cantidadDisponible=9 WHERE id=@id;", ("@id", datos.Lote2));

        await Assert.ThrowsAsync<RestauracionExcedeCantidadInicialException>(() =>
            db.Anulacion(sesion, datos).AnularVentaAsync(venta.IdVenta, "Prueba de límite"));

        Assert.Equal(0, await db.Disponible(datos.Lote1));
        Assert.Equal(9, await db.Disponible(datos.Lote2));
        Assert.Equal(9, await db.Stock(datos.Producto));
        Assert.Equal("COMPLETADA", await db.EscalarAsync<string>("SELECT estado FROM VENTA WHERE id=@id;", ("@id", venta.IdVenta)));
    }

    [MySqlIntegrationFact]
    public async Task Dos_ventas_concurrentes_no_venden_mas_que_el_stock()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Intentar()
        {
            await using var sesion = new PersistenciaVentasMySql(db.Conexiones);
            await salida.Task;
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await db.Registro(sesion, datos).RegistrarVentaAsync(
                    new NuevaVentaDto(datos.Cliente, [new LineaVentaDto(datos.Producto, 8)]), limite.Token);
                return true;
            }
            catch (StockInsuficienteException) { return false; }
        }
        var primera = Intentar();
        var segunda = Intentar();
        salida.SetResult();
        var resultados = await Task.WhenAll(primera, segunda);

        Assert.Equal(1, resultados.Count(r => r));
        Assert.Equal(1, await db.CantidadVentas(datos.Cliente));
        Assert.Equal(5, await db.Stock(datos.Producto));
        Assert.Equal(0, await db.Disponible(datos.Lote1));
        Assert.Equal(5, await db.Disponible(datos.Lote2));
    }

    [MySqlIntegrationFact]
    public async Task Dos_anulaciones_concurrentes_solo_restauran_una_vez()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        int id;
        await using (var sesion = new PersistenciaVentasMySql(db.Conexiones))
            id = (await db.Registro(sesion, datos).RegistrarVentaAsync(
                new NuevaVentaDto(datos.Cliente, [new LineaVentaDto(datos.Producto, 5)]))).IdVenta;
        var salida = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Intentar()
        {
            await using var sesion = new PersistenciaVentasMySql(db.Conexiones);
            await salida.Task;
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await db.Anulacion(sesion, datos).AnularVentaAsync(id, "Concurrente", limite.Token);
                return true;
            }
            catch (InvalidOperationException ex) when (ex.Message == "La venta no existe o ya fue anulada.") { return false; }
        }
        var primera = Intentar();
        var segunda = Intentar();
        salida.SetResult();
        var resultados = await Task.WhenAll(primera, segunda);

        Assert.Equal(1, resultados.Count(r => r));
        Assert.Equal(13, await db.Stock(datos.Producto));
        Assert.Equal(3, await db.Disponible(datos.Lote1));
        Assert.Equal(10, await db.Disponible(datos.Lote2));
    }

    [MySqlIntegrationFact]
    public async Task Dispose_revierte_una_transaccion_incompleta_y_libera_su_conexion()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        var datos = await db.SembrarAsync();
        await using (var sesion = new PersistenciaVentasMySql(db.Conexiones))
        {
            await sesion.Unidad.IniciarAsync();
            await sesion.Ventas.InsertarCabeceraAsync(datos.Cliente, datos.Usuario);
            await sesion.Lotes.DescontarAsync(datos.Lote1, 2);
        }

        Assert.Equal(0, await db.CantidadVentas(datos.Cliente));
        Assert.Equal(3, await db.Disponible(datos.Lote1));
        Assert.Equal(13, await db.Stock(datos.Producto));
    }

    [MySqlIntegrationFact]
    public async Task La_sesion_reutiliza_la_misma_conexion_y_transaccion_y_rechaza_anidamiento()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        await using var sesion = new PersistenciaVentasMySql(db.Conexiones);
        await sesion.Unidad.IniciarAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sesion.Unidad.IniciarAsync());
        await using (var primero = await sesion.Unidad.AlquilarAsync(CancellationToken.None, true))
        await using (var segundo = await sesion.Unidad.AlquilarAsync(CancellationToken.None, true))
        {
            Assert.Same(primero.Conexion, segundo.Conexion);
            Assert.Same(primero.Transaccion, segundo.Transaccion);
            Assert.NotNull(primero.Transaccion);
        }
        await sesion.Unidad.RevertirAsync();
        Assert.False(sesion.Unidad.HayTransaccionActiva);
        await sesion.Unidad.IniciarAsync();
        await sesion.Unidad.ConfirmarAsync();
    }

    [MySqlIntegrationFact]
    public async Task El_detector_reconoce_el_codigo_mysql_1213_incluso_envuelto_pero_no_1205()
    {
        var db = await MySqlTestEnvironment.CrearAsync();
        // Errores reales del proveedor, sin provocar ni simular cambios de datos.
        var deadlock = await Assert.ThrowsAsync<MySqlException>(() => db.EjecutarAsync(
            "SIGNAL SQLSTATE '40001' SET MYSQL_ERRNO=1213, MESSAGE_TEXT='Prueba de clasificador';"));
        var timeout = await Assert.ThrowsAsync<MySqlException>(() => db.EjecutarAsync(
            "SIGNAL SQLSTATE 'HY000' SET MYSQL_ERRNO=1205, MESSAGE_TEXT='Prueba de timeout';"));
        var detector = new DetectorDeInterbloqueoMySql();
        Assert.True(detector.EsInterbloqueo(deadlock));
        Assert.True(detector.EsInterbloqueo(new Exception("envoltura", deadlock)));
        Assert.False(detector.EsInterbloqueo(timeout));
    }
}
