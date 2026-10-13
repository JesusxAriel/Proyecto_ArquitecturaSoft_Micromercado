using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Domain.Sales;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlVentaConsultaRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IVentaConsultaRepository
{
    public Task<IReadOnlyList<VentaResumenDto>> ListarAsync(
        bool incluirAnuladas,
        int limite,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT v.id, v.nroComprobante, v.fechaVenta, c.razonSocial, v.estado, v.montoTotal
            FROM VENTA v
            INNER JOIN CLIENTE c ON c.id = v.idCliente
            WHERE (@incluirAnuladas = 1 OR v.estado = @estadoCompletada)
            ORDER BY v.fechaVenta DESC, v.id DESC
            LIMIT @limite;
            """;

        return ErroresDePersistencia.TraducirAsync<IReadOnlyList<VentaResumenDto>>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
            await using var comando = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
            comando.Parameters.AddWithValue("@incluirAnuladas", incluirAnuladas ? 1 : 0);
            comando.Parameters.AddWithValue("@estadoCompletada", EstadoVenta.Completada);
            comando.Parameters.AddWithValue("@limite", limite);

            await using var reader = await comando.ExecuteReaderAsync(cancellationToken);
            var ventas = new List<VentaResumenDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                ventas.Add(new VentaResumenDto(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("nroComprobante")),
                    reader.GetDateTime(reader.GetOrdinal("fechaVenta")),
                    reader.GetString(reader.GetOrdinal("razonSocial")),
                    reader.GetString(reader.GetOrdinal("estado")),
                    reader.GetDecimal(reader.GetOrdinal("montoTotal"))));
            }

            return ventas;
        });
    }

    public Task<VentaDetalleConFilasDto?> ObtenerDetalleAsync(
        int idVenta,
        CancellationToken cancellationToken = default)
    {
        const string cabeceraSql = """
            SELECT v.id, v.nroComprobante, v.fechaVenta, v.estado, v.montoTotal, v.efectivoPagado, v.cambio,
                   v.motivoAnulacion, v.fechaAnulacion,
                   c.nitCi, c.razonSocial,
                   u.nombreCompleto AS usuarioCajero,
                   ua.nombreCompleto AS usuarioAnulacion
            FROM VENTA v
            INNER JOIN CLIENTE c ON c.id = v.idCliente
            INNER JOIN USUARIO u ON u.id = v.idUsuario
            LEFT JOIN USUARIO ua ON ua.id = v.idUsuarioAnulacion
            WHERE v.id = @idVenta;
            """;
        const string detalleSql = """
            SELECT dv.idProducto, p.nombre, dv.cantidad, dv.precioUnitario, dv.subtotal
            FROM DETALLE_VENTA dv
            INNER JOIN PRODUCTO p ON p.id = dv.idProducto
            WHERE dv.idVenta = @idVenta
            ORDER BY dv.id;
            """;

        return ErroresDePersistencia.TraducirAsync<VentaDetalleConFilasDto?>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

            int id;
            string nroComprobante, estado, nitCi, razonSocial, usuarioCajero;
            string? motivoAnulacion, usuarioAnulacion;
            DateTime fechaVenta;
            DateTime? fechaAnulacion;
            decimal montoTotal, efectivoPagado, cambio;

            await using (var cabecera = new MySqlCommand(cabeceraSql, alquiler.Conexion, alquiler.Transaccion))
            {
                cabecera.Parameters.AddWithValue("@idVenta", idVenta);
                await using var reader = await cabecera.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                var ordMotivo = reader.GetOrdinal("motivoAnulacion");
                var ordFechaAnulacion = reader.GetOrdinal("fechaAnulacion");
                var ordUsuarioAnulacion = reader.GetOrdinal("usuarioAnulacion");

                id = reader.GetInt32(reader.GetOrdinal("id"));
                nroComprobante = reader.GetString(reader.GetOrdinal("nroComprobante"));
                fechaVenta = reader.GetDateTime(reader.GetOrdinal("fechaVenta"));
                estado = reader.GetString(reader.GetOrdinal("estado"));
                montoTotal = reader.GetDecimal(reader.GetOrdinal("montoTotal"));
                efectivoPagado = reader.GetDecimal(reader.GetOrdinal("efectivoPagado"));
                cambio = reader.GetDecimal(reader.GetOrdinal("cambio"));
                motivoAnulacion = reader.IsDBNull(ordMotivo) ? null : reader.GetString(ordMotivo);
                fechaAnulacion = reader.IsDBNull(ordFechaAnulacion) ? null : reader.GetDateTime(ordFechaAnulacion);
                nitCi = reader.GetString(reader.GetOrdinal("nitCi"));
                razonSocial = reader.GetString(reader.GetOrdinal("razonSocial"));
                usuarioCajero = reader.GetString(reader.GetOrdinal("usuarioCajero"));
                usuarioAnulacion = reader.IsDBNull(ordUsuarioAnulacion) ? null : reader.GetString(ordUsuarioAnulacion);
            }

            var filas = new List<FilaDetalleVenta>();

            await using (var comando = new MySqlCommand(detalleSql, alquiler.Conexion, alquiler.Transaccion))
            {
                comando.Parameters.AddWithValue("@idVenta", idVenta);
                await using var reader = await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    filas.Add(new FilaDetalleVenta(
                        reader.GetInt32(reader.GetOrdinal("idProducto")),
                        reader.GetString(reader.GetOrdinal("nombre")),
                        reader.GetInt32(reader.GetOrdinal("cantidad")),
                        reader.GetDecimal(reader.GetOrdinal("precioUnitario")),
                        reader.GetDecimal(reader.GetOrdinal("subtotal"))));
                }
            }

            return new VentaDetalleConFilasDto(
                id,
                nroComprobante,
                fechaVenta,
                nitCi,
                razonSocial,
                usuarioCajero,
                estado,
                montoTotal,
                efectivoPagado,
                cambio,
                motivoAnulacion,
                fechaAnulacion,
                usuarioAnulacion,
                filas);
        });
    }
}
