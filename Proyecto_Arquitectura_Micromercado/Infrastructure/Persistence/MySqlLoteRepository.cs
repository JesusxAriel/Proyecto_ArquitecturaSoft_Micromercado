using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Domain.Stock;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlLoteRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : ILoteRepository
{
    public async Task<IReadOnlyList<LoteDisponible>> ObtenerVigentesParaDescuentoAsync(int idProducto, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            SELECT id, cantidadDisponible, fechaVencimiento
            FROM LOTE
            WHERE idProducto = @idProducto AND estaActivo = 1 AND cantidadDisponible > 0
            FOR UPDATE;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var ordId = reader.GetOrdinal("id");
        var ordCantidad = reader.GetOrdinal("cantidadDisponible");
        var ordVencimiento = reader.GetOrdinal("fechaVencimiento");

        var lotes = new List<LoteDisponible>();
        while (await reader.ReadAsync(cancellationToken))
        {
            DateOnly? vencimiento = reader.IsDBNull(ordVencimiento)
                ? null
                : DateOnly.FromDateTime(reader.GetDateTime(ordVencimiento));

            lotes.Add(new LoteDisponible(reader.GetInt32(ordId), reader.GetInt32(ordCantidad), vencimiento));
        }

        return lotes;
    }

    public async Task DescontarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            UPDATE LOTE
            SET cantidadDisponible = cantidadDisponible - @cantidad
            WHERE id = @idLote AND cantidadDisponible >= @cantidad;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@idLote", idLote);

        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (filas == 0) throw new StockInsuficienteException();
    }

    public async Task RestaurarAsync(int idLote, int cantidad, CancellationToken cancellationToken = default)
    {
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);

        const string sql = @"
            UPDATE LOTE
            SET cantidadDisponible = cantidadDisponible + @cantidad
            WHERE id = @idLote AND cantidadDisponible + @cantidad <= cantidadInicial;";

        await using var cmd = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@idLote", idLote);

        var filas = await cmd.ExecuteNonQueryAsync(cancellationToken);
        if (filas == 0) throw new RestauracionExcedeCantidadInicialException();
    }
}
