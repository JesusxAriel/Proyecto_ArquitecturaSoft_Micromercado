using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using System.Data;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlPriceHistoryRepository : IPriceHistoryRepository
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
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
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

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await AddPriceHistoryAsync(connection, null, history, cancellationToken);
    }

    public async Task AddPriceHistoryAsync(IDbConnection connection, IDbTransaction? transaction, ProductPriceHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (connection is not MySqlConnection mySqlConnection)
        {
            throw new ArgumentException("Connection must be a MySqlConnection.", nameof(connection));
        }

        var mySqlTransaction = transaction as MySqlTransaction;

        const string sql = """
            INSERT INTO HISTORIAL_PRECIO
                (idProducto, precioVentaAnterior, precioVentaNuevo,
                 precioCostoAnterior, precioCostoNuevo, motivoCambio, idUsuario)
            VALUES
                (@idProducto, @precioVentaAnterior, @precioVentaNuevo,
                 @precioCostoAnterior, @precioCostoNuevo, @motivoCambio, @idUsuario);
            """;

        await using var command = new MySqlCommand(sql, mySqlConnection, mySqlTransaction);
        AddPriceHistoryParameters(command, history);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
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
