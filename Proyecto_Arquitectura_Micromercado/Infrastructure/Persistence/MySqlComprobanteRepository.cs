using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Comprobantes;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlComprobanteRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IComprobanteRepository
{
    public Task<VentaParaComprobante?> ObtenerPorIdVentaAsync(
        int idVenta,
        CancellationToken cancellationToken = default)
    {
        const string cabeceraSql = """
            SELECT v.id, v.nroComprobante, v.fechaVenta, v.estado, v.montoTotal,
                   c.nitCi, c.razonSocial, u.username AS usuarioCajero
            FROM VENTA v
            INNER JOIN CLIENTE c ON c.id = v.idCliente
            INNER JOIN USUARIO u ON u.id = v.idUsuario
            WHERE v.id = @idVenta;
            """;
        const string detalleSql = """
            SELECT dv.idProducto, p.nombre, dv.cantidad, dv.precioUnitario, dv.subtotal
            FROM DETALLE_VENTA dv
            INNER JOIN PRODUCTO p ON p.id = dv.idProducto
            WHERE dv.idVenta = @idVenta
            ORDER BY dv.id;
            """;

        return ErroresDePersistencia.TraducirAsync<VentaParaComprobante?>(async () =>
        {
            await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

            int id;
            string nroComprobante, estado, nitCi, razonSocial, usuarioCajero;
            DateTime fechaVenta;
            decimal montoTotal;

            await using (var cabecera = new MySqlCommand(cabeceraSql, alquiler.Conexion, alquiler.Transaccion))
            {
                cabecera.Parameters.AddWithValue("@idVenta", idVenta);
                await using var reader = await cabecera.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                id = reader.GetInt32(reader.GetOrdinal("id"));
                nroComprobante = reader.GetString(reader.GetOrdinal("nroComprobante"));
                fechaVenta = reader.GetDateTime(reader.GetOrdinal("fechaVenta"));
                estado = reader.GetString(reader.GetOrdinal("estado"));
                montoTotal = reader.GetDecimal(reader.GetOrdinal("montoTotal"));
                nitCi = reader.GetString(reader.GetOrdinal("nitCi"));
                razonSocial = reader.GetString(reader.GetOrdinal("razonSocial"));
                usuarioCajero = reader.GetString(reader.GetOrdinal("usuarioCajero"));
            }

            var detalle = new List<DetalleDeVenta>();

            await using (var comando = new MySqlCommand(detalleSql, alquiler.Conexion, alquiler.Transaccion))
            {
                comando.Parameters.AddWithValue("@idVenta", idVenta);
                await using var reader = await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    detalle.Add(new DetalleDeVenta(
                        reader.GetInt32(reader.GetOrdinal("idProducto")),
                        reader.GetString(reader.GetOrdinal("nombre")),
                        reader.GetInt32(reader.GetOrdinal("cantidad")),
                        reader.GetDecimal(reader.GetOrdinal("precioUnitario")),
                        reader.GetDecimal(reader.GetOrdinal("subtotal"))));
                }
            }

            return new VentaParaComprobante(
                id,
                nroComprobante,
                fechaVenta,
                nitCi,
                razonSocial,
                estado,
                montoTotal,
                usuarioCajero,
                detalle);
        });
    }
}
