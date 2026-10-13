using System.Data;
using System.Text.Json;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlVentaRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IVentaRepository
{
    public async Task<(int IdVenta, string NroComprobante, decimal Total)> RegistrarVentaConSpAsync(NuevaVentaDto dto, int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        var detallesJson = JsonSerializer.Serialize(dto.Lineas.Select(l => new
        {
            idProducto = l.IdProducto,
            cantidad = l.Cantidad
        }));

        await using var cmd = new MySqlCommand("sp_RegistrarVenta", alquiler.Conexion, alquiler.Transaccion)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_idCliente", dto.IdCliente);
        cmd.Parameters.AddWithValue("p_idUsuario", idUsuario);
        cmd.Parameters.AddWithValue("p_efectivoPagado", dto.EfectivoPagado);
        cmd.Parameters.AddWithValue("p_detalles", detallesJson);

        var pIdVenta = new MySqlParameter("p_idVenta", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        var pNroComprobante = new MySqlParameter("p_nroComprobante", MySqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };

        cmd.Parameters.Add(pIdVenta);
        cmd.Parameters.Add(pNroComprobante);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        int idVenta = Convert.ToInt32(pIdVenta.Value);
        string nroComprobante = pNroComprobante.Value?.ToString() ?? string.Empty;

        // Obtener el total generado automáticamente por el Stored Procedure
        const string sqlMonto = "SELECT montoTotal FROM VENTA WHERE id = @idVenta;";
        await using var cmdMonto = new MySqlCommand(sqlMonto, alquiler.Conexion, alquiler.Transaccion);
        cmdMonto.Parameters.AddWithValue("@idVenta", idVenta);
        var resultMonto = await cmdMonto.ExecuteScalarAsync(cancellationToken);
        decimal total = resultMonto != null && resultMonto != DBNull.Value ? Convert.ToDecimal(resultMonto) : 0.00m;

        return (idVenta, nroComprobante, total);
    }

    public async Task<bool> AnularVentaConSpAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        await using var cmd = new MySqlCommand("sp_AnularVenta", alquiler.Conexion, alquiler.Transaccion)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_idVenta", idVenta);
        cmd.Parameters.AddWithValue("p_idUsuarioAnulacion", idUsuarioAnulacion);
        cmd.Parameters.AddWithValue("p_motivo", motivo);

        // Al tratarse de un Stored Procedure, si no lanza excepción, la anulación fue exitosa
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<Venta?> GetByIdAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sqlVenta = @"
            SELECT id, nroComprobante, fechaVenta, montoTotal, estado, motivoAnulacion, idCliente, idUsuario
            FROM VENTA
            WHERE id = @idVenta;";

        await using var cmdVenta = new MySqlCommand(sqlVenta, alquiler.Conexion, alquiler.Transaccion);
        cmdVenta.Parameters.AddWithValue("@idVenta", idVenta);

        await using var reader = await cmdVenta.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var ordId = reader.GetOrdinal("id");
        var ordNro = reader.GetOrdinal("nroComprobante");
        var ordFecha = reader.GetOrdinal("fechaVenta");
        var ordTotal = reader.GetOrdinal("montoTotal");
        var ordEstado = reader.GetOrdinal("estado");
        var ordMotivo = reader.GetOrdinal("motivoAnulacion");
        var ordCliente = reader.GetOrdinal("idCliente");
        var ordUsuario = reader.GetOrdinal("idUsuario");

        var venta = new Venta
        {
            Id = reader.GetInt32(ordId),
            NroComprobante = reader.GetString(ordNro),
            FechaHora = reader.GetDateTime(ordFecha),
            Total = reader.GetDecimal(ordTotal),
            Estado = reader.GetString(ordEstado),
            MotivoAnulacion = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo),
            IdCliente = reader.GetInt32(ordCliente),
            IdUsuario = reader.GetInt32(ordUsuario)
        };
        await reader.CloseAsync();

        const string sqlDetalle = @"
            SELECT d.id, d.idVenta, d.idProducto, d.idLote, p.nombre AS nombreProducto, d.cantidad, d.precioUnitario
            FROM DETALLE_VENTA d
            INNER JOIN PRODUCTO p ON d.idProducto = p.id
            WHERE d.idVenta = @idVenta;";

        await using var cmdDetalle = new MySqlCommand(sqlDetalle, alquiler.Conexion, alquiler.Transaccion);
        cmdDetalle.Parameters.AddWithValue("@idVenta", idVenta);

        await using var readerDetalle = await cmdDetalle.ExecuteReaderAsync(cancellationToken);
        var ordDetId = readerDetalle.GetOrdinal("id");
        var ordDetVenta = readerDetalle.GetOrdinal("idVenta");
        var ordDetProducto = readerDetalle.GetOrdinal("idProducto");
        var ordDetLote = readerDetalle.GetOrdinal("idLote");
        var ordDetNombre = readerDetalle.GetOrdinal("nombreProducto");
        var ordDetCantidad = readerDetalle.GetOrdinal("cantidad");
        var ordDetPrecio = readerDetalle.GetOrdinal("precioUnitario");

        while (await readerDetalle.ReadAsync(cancellationToken))
        {
            venta.Detalles.Add(new DetalleVenta
            {
                Id = readerDetalle.GetInt32(ordDetId),
                IdVenta = readerDetalle.GetInt32(ordDetVenta),
                IdProducto = readerDetalle.GetInt32(ordDetProducto),
                IdLote = readerDetalle.GetInt32(ordDetLote),
                NombreProducto = readerDetalle.GetString(ordDetNombre),
                Cantidad = readerDetalle.GetInt32(ordDetCantidad),
                PrecioUnitario = readerDetalle.GetDecimal(ordDetPrecio)
            });
        }

        return venta;
    }

    public async Task<IReadOnlyList<Venta>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        var ventas = new List<Venta>();

        const string sql = @"
            SELECT id, nroComprobante, fechaVenta, montoTotal, estado, motivoAnulacion, idCliente, idUsuario
            FROM VENTA
            ORDER BY fechaVenta DESC;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var ordId = reader.GetOrdinal("id");
        var ordNro = reader.GetOrdinal("nroComprobante");
        var ordFecha = reader.GetOrdinal("fechaVenta");
        var ordTotal = reader.GetOrdinal("montoTotal");
        var ordEstado = reader.GetOrdinal("estado");
        var ordMotivo = reader.GetOrdinal("motivoAnulacion");
        var ordCliente = reader.GetOrdinal("idCliente");
        var ordUsuario = reader.GetOrdinal("idUsuario");

        while (await reader.ReadAsync(cancellationToken))
        {
            ventas.Add(new Venta
            {
                Id = reader.GetInt32(ordId),
                NroComprobante = reader.GetString(ordNro),
                FechaHora = reader.GetDateTime(ordFecha),
                Total = reader.GetDecimal(ordTotal),
                Estado = reader.GetString(ordEstado),
                MotivoAnulacion = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo),
                IdCliente = reader.GetInt32(ordCliente),
                IdUsuario = reader.GetInt32(ordUsuario)
            });
        }

        return ventas;
    }

    public async Task<decimal> GetVentasDelDiaAsync(CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            SELECT COALESCE(SUM(montoTotal), 0)
            FROM VENTA
            WHERE DATE(fechaVenta) = CURDATE()
            AND estado = 'COMPLETADA';";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);

        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
    }

    public async Task<int> InsertarCabeceraAsync(int idCliente, int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            INSERT INTO VENTA (nroComprobante, idCliente, idUsuario, fechaVenta, montoTotal, efectivoPagado, cambio, estado)
            VALUES (CONCAT('TMP-', UUID()), @idCliente, @idUsuario, NOW(), 0, 0, 0, 'COMPLETADA');";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@idCliente", idCliente);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await using var cmdId = new MySqlCommand("SELECT LAST_INSERT_ID();", alquiler.Conexion, alquiler.Transaccion);
        return Convert.ToInt32(await cmdId.ExecuteScalarAsync(cancellationToken));
    }

    public async Task InsertarDetalleAsync(int idVenta, int idProducto, int idLote, int cantidad, decimal precioUnitario, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            INSERT INTO DETALLE_VENTA (idVenta, idProducto, idLote, cantidad, precioUnitario)
            VALUES (@idVenta, @idProducto, @idLote, @cantidad, @precioUnitario);";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@idVenta", idVenta);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);
        cmd.Parameters.AddWithValue("@idLote", idLote);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@precioUnitario", precioUnitario);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task CerrarVentaAsync(int idVenta, string nroComprobante, decimal montoTotal, decimal efectivoPagado, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            UPDATE VENTA
            SET nroComprobante = @nro, montoTotal = @total, efectivoPagado = @efectivo, cambio = @efectivo - @total
            WHERE id = @idVenta;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@nro", nroComprobante);
        cmd.Parameters.AddWithValue("@total", montoTotal);
        cmd.Parameters.AddWithValue("@efectivo", efectivoPagado);
        cmd.Parameters.AddWithValue("@idVenta", idVenta);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Venta?> ObtenerParaAnularAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sqlVenta = @"
            SELECT id, nroComprobante, fechaVenta, montoTotal, estado, motivoAnulacion, idCliente, idUsuario
            FROM VENTA
            WHERE id = @idVenta
            FOR UPDATE;";

        await using var cmdVenta = new MySqlCommand(sqlVenta, alquiler.Conexion, alquiler.Transaccion);
        cmdVenta.Parameters.AddWithValue("@idVenta", idVenta);

        await using var reader = await cmdVenta.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var ordMotivo = reader.GetOrdinal("motivoAnulacion");

        var venta = new Venta
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            NroComprobante = reader.GetString(reader.GetOrdinal("nroComprobante")),
            FechaHora = reader.GetDateTime(reader.GetOrdinal("fechaVenta")),
            Total = reader.GetDecimal(reader.GetOrdinal("montoTotal")),
            Estado = reader.GetString(reader.GetOrdinal("estado")),
            MotivoAnulacion = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo),
            IdCliente = reader.GetInt32(reader.GetOrdinal("idCliente")),
            IdUsuario = reader.GetInt32(reader.GetOrdinal("idUsuario"))
        };
        await reader.CloseAsync();

        const string sqlDetalle = @"
            SELECT id, idVenta, idProducto, idLote, cantidad, precioUnitario
            FROM DETALLE_VENTA
            WHERE idVenta = @idVenta;";

        await using var cmdDetalle = new MySqlCommand(sqlDetalle, alquiler.Conexion, alquiler.Transaccion);
        cmdDetalle.Parameters.AddWithValue("@idVenta", idVenta);

        await using var readerDetalle = await cmdDetalle.ExecuteReaderAsync(cancellationToken);
        var ordDetId = readerDetalle.GetOrdinal("id");
        var ordDetVenta = readerDetalle.GetOrdinal("idVenta");
        var ordDetProducto = readerDetalle.GetOrdinal("idProducto");
        var ordDetLote = readerDetalle.GetOrdinal("idLote");
        var ordDetCantidad = readerDetalle.GetOrdinal("cantidad");
        var ordDetPrecio = readerDetalle.GetOrdinal("precioUnitario");

        while (await readerDetalle.ReadAsync(cancellationToken))
        {
            venta.Detalles.Add(new DetalleVenta
            {
                Id = readerDetalle.GetInt32(ordDetId),
                IdVenta = readerDetalle.GetInt32(ordDetVenta),
                IdProducto = readerDetalle.GetInt32(ordDetProducto),
                IdLote = readerDetalle.GetInt32(ordDetLote),
                Cantidad = readerDetalle.GetInt32(ordDetCantidad),
                PrecioUnitario = readerDetalle.GetDecimal(ordDetPrecio)
            });
        }

        return venta;
    }

    public async Task MarcarAnuladaAsync(int idVenta, string motivo, int idUsuarioAnulacion, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            UPDATE VENTA
            SET estado = 'ANULADA', motivoAnulacion = @motivo, idUsuarioAnulacion = @idUsuario, fechaAnulacion = NOW()
            WHERE id = @idVenta;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@motivo", motivo);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuarioAnulacion);
        cmd.Parameters.AddWithValue("@idVenta", idVenta);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}