using System.Data;
using System.Text.Json;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlVentaRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IVentaRepository
{
    public async Task<(int IdVenta, string NroComprobante)> RegistrarVentaConSpAsync(NuevaVentaDto dto, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        // Serializar las líneas de venta al formato JSON que requiere sp_RegistrarVenta
        var detallesJson = JsonSerializer.Serialize(dto.Lineas.Select(l => new
        {
            idProducto = l.IdProducto,
            cantidad = l.Cantidad
        }));

        await using var cmd = new MySqlCommand("sp_RegistrarVenta", alquiler.Conexion, alquiler.Transaccion)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_idCliente", dto.IdCliente > 0 ? dto.IdCliente : 1);
        cmd.Parameters.AddWithValue("p_idUsuario", 1);
        cmd.Parameters.AddWithValue("p_efectivoPagado", 999999.00m);
        cmd.Parameters.AddWithValue("p_detalles", detallesJson);

        var pIdVenta = new MySqlParameter("p_idVenta", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        var pNroComprobante = new MySqlParameter("p_nroComprobante", MySqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };

        cmd.Parameters.Add(pIdVenta);
        cmd.Parameters.Add(pNroComprobante);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        int idVenta = Convert.ToInt32(pIdVenta.Value);
        string nroComprobante = pNroComprobante.Value?.ToString() ?? string.Empty;

        return (idVenta, nroComprobante);
    }

   public async Task<bool> AnularVentaConSpAsync(int idVenta, string motivo, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        await using var cmd = new MySqlCommand("sp_AnularVenta", alquiler.Conexion, alquiler.Transaccion)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_idVenta", idVenta);
        cmd.Parameters.AddWithValue("p_idUsuarioAnulacion", 1);
        cmd.Parameters.AddWithValue("p_motivo", motivo);

        // Al tratarse de un Stored Procedure, si no lanza excepción, la anulación fue exitosa
        await cmd.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task<int> CrearVentaAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            INSERT INTO VENTA (nroComprobante, fechaVenta, montoTotal, efectivoPagado, cambio, estado, idCliente, idUsuario)
            VALUES (@nroComprobante, @fechaVenta, @montoTotal, @efectivoPagado, @cambio, @estado, @idCliente, @idUsuario);
            SELECT LAST_INSERT_ID();";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@nroComprobante", venta.NroComprobante);
        cmd.Parameters.AddWithValue("@fechaVenta", venta.FechaHora);
        cmd.Parameters.AddWithValue("@montoTotal", venta.Total);
        cmd.Parameters.AddWithValue("@efectivoPagado", venta.Total);
        cmd.Parameters.AddWithValue("@cambio", 0.00m);
        cmd.Parameters.AddWithValue("@estado", string.Equals(venta.Estado, "COMPLETADO", StringComparison.OrdinalIgnoreCase) ? "COMPLETADA" : venta.Estado);
        cmd.Parameters.AddWithValue("@idCliente", venta.IdCliente);
        cmd.Parameters.AddWithValue("@idUsuario", venta.IdUsuario);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        venta.Id = Convert.ToInt32(result);
        return venta.Id;
    }

    public async Task CrearDetalleVentaAsync(DetalleVenta detalle, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            INSERT INTO DETALLE_VENTA (idVenta, idProducto, cantidad, precioUnitario)
            VALUES (@idVenta, @idProducto, @cantidad, @precioUnitario);";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@idVenta", detalle.IdVenta);
        cmd.Parameters.AddWithValue("@idProducto", detalle.IdProducto);
        cmd.Parameters.AddWithValue("@cantidad", detalle.Cantidad);
        cmd.Parameters.AddWithValue("@precioUnitario", detalle.PrecioUnitario);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
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
            SELECT d.id, d.idVenta, d.idProducto, p.nombre AS nombreProducto, d.cantidad, d.precioUnitario
            FROM DETALLE_VENTA d
            INNER JOIN PRODUCTO p ON d.idProducto = p.id
            WHERE d.idVenta = @idVenta;";

        await using var cmdDetalle = new MySqlCommand(sqlDetalle, alquiler.Conexion, alquiler.Transaccion);
        cmdDetalle.Parameters.AddWithValue("@idVenta", idVenta);

        await using var readerDetalle = await cmdDetalle.ExecuteReaderAsync(cancellationToken);
        var ordDetId = readerDetalle.GetOrdinal("id");
        var ordDetVenta = readerDetalle.GetOrdinal("idVenta");
        var ordDetProducto = readerDetalle.GetOrdinal("idProducto");
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

    public async Task<bool> CambiarEstadoAnuladoAsync(int idVenta, string motivo, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            UPDATE VENTA 
            SET estado = 'ANULADA', 
                motivoAnulacion = @motivo,
                idUsuarioAnulacion = 1,
                fechaAnulacion = NOW()
            WHERE id = @idVenta;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@motivo", motivo);
        cmd.Parameters.AddWithValue("@idVenta", idVenta);

        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}