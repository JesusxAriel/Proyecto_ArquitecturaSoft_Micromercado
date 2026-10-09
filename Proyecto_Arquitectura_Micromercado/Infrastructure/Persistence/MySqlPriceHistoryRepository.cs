using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

// Recibe la unidad de trabajo en vez de la conexion suelta: cuando el repositorio de
// producto abre una transaccion, esta insercion queda dentro de ella sin necesidad de
// que nadie le pase la transaccion por parametro.
public sealed class MySqlPriceHistoryRepository(MySqlUnidadDeTrabajo unidadDeTrabajo) : IPriceHistoryRepository
{
    public async Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT h.id, h.idProducto, p.nombre AS NombreProducto,
                   h.precioVentaAnterior, h.precioVentaNuevo,
                   h.precioCostoAnterior, h.precioCostoNuevo,
                   h.motivoCambio, h.idUsuario, h.fechaCambio
            FROM HISTORIAL_PRECIO h
            INNER JOIN PRODUCTO p ON p.id = h.idProducto
            ORDER BY h.fechaCambio DESC;
            """;

        var history = new List<ProductPriceHistory>();
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new ProductPriceHistory
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                IdProducto = reader.GetInt32(reader.GetOrdinal("idProducto")),
                NombreProducto = reader.GetString(reader.GetOrdinal("NombreProducto")),
                PrecioVentaAnterior = reader.GetDecimal(reader.GetOrdinal("precioVentaAnterior")),
                PrecioVentaNuevo = reader.GetDecimal(reader.GetOrdinal("precioVentaNuevo")),
                PrecioCostoAnterior = reader.IsDBNull(reader.GetOrdinal("precioCostoAnterior"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("precioCostoAnterior")),
                PrecioCostoNuevo = reader.IsDBNull(reader.GetOrdinal("precioCostoNuevo"))
                    ? null
                    : reader.GetDecimal(reader.GetOrdinal("precioCostoNuevo")),
                MotivoCambio = reader.IsDBNull(reader.GetOrdinal("motivoCambio"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("motivoCambio")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("idUsuario")),
                FechaCambio = reader.GetDateTime(reader.GetOrdinal("fechaCambio"))
            });
        }

        return history;
    }

    public async Task AddPriceHistoryAsync(ProductPriceHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        const string sql = """
            INSERT INTO HISTORIAL_PRECIO
                (idProducto, precioVentaAnterior, precioVentaNuevo,
                 precioCostoAnterior, precioCostoNuevo, motivoCambio, idUsuario)
            VALUES
                (@idProducto, @precioVentaAnterior, @precioVentaNuevo,
                 @precioCostoAnterior, @precioCostoNuevo, @motivoCambio, @idUsuario);
            """;

        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        AddPriceHistoryParameters(command, history);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPriceHistoryParameters(MySqlCommand command, ProductPriceHistory history)
    {
        command.Parameters.AddWithValue("@idProducto", history.IdProducto);
        command.Parameters.AddWithValue("@precioVentaAnterior", history.PrecioVentaAnterior);
        command.Parameters.AddWithValue("@precioVentaNuevo", history.PrecioVentaNuevo);
        command.Parameters.AddWithValue("@precioCostoAnterior", (object?)history.PrecioCostoAnterior ?? DBNull.Value);
        command.Parameters.AddWithValue("@precioCostoNuevo", (object?)history.PrecioCostoNuevo ?? DBNull.Value);
        command.Parameters.AddWithValue("@motivoCambio", history.MotivoCambio);
        command.Parameters.AddWithValue("@idUsuario", history.IdUsuario);
    }
}
