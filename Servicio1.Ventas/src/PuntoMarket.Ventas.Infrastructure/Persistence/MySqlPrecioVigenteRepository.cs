using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Products;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

public sealed class MySqlPrecioVigenteRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IConPrecioVigente
{
    public async Task<decimal?> ObtenerPrecioVigenteAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT precioVenta
            FROM PRODUCTO
            WHERE id = @idProducto
              AND estaActivo = 1
            FOR UPDATE;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken, requiereTransaccion: true);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@idProducto", idProducto);
        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is null || resultado == DBNull.Value ? null : Convert.ToDecimal(resultado);
    }

    public async Task<DescripcionDeProducto?> ObtenerDescripcionAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT p.nombre, e.nombre AS empaquePresentacion
            FROM PRODUCTO p
            INNER JOIN EMPAQUE e ON e.id = p.idEmpaque
            WHERE p.id = @idProducto;
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        command.Parameters.AddWithValue("@idProducto", idProducto);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new DescripcionDeProducto(reader.GetString(reader.GetOrdinal("nombre")), reader.GetString(reader.GetOrdinal("empaquePresentacion")))
            : null;
    }

}
