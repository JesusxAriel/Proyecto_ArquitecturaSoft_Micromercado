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

        return await GetHistoryAsync(sql, null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryByProductIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        const string sql = """
            SELECT h.id, h.idProducto, p.nombre AS NombreProducto,
                   h.precioVentaAnterior, h.precioVentaNuevo,
                   h.precioCostoAnterior, h.precioCostoNuevo,
                   h.motivoCambio, h.idUsuario, h.fechaCambio
            FROM HISTORIAL_PRECIO h
            INNER JOIN PRODUCTO p ON p.id = h.idProducto
            WHERE h.idProducto = @idProducto
            ORDER BY h.fechaCambio DESC;
            """;

        return await GetHistoryAsync(sql, productId, cancellationToken);
    }

    private async Task<IReadOnlyList<ProductPriceHistory>> GetHistoryAsync(
        string sql,
        int? productId,
        CancellationToken cancellationToken)
    {
        var history = new List<ProductPriceHistory>();
        await using var alquiler = await unidadDeTrabajo.AlquilarAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, alquiler.Conexion, alquiler.Transaccion);
        if (productId.HasValue)
        {
            command.Parameters.AddWithValue("@idProducto", productId.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ordId = reader.GetOrdinal("id");
        var ordIdProducto = reader.GetOrdinal("idProducto");
        var ordNombreProducto = reader.GetOrdinal("NombreProducto");
        var ordPrecioVentaAnterior = reader.GetOrdinal("precioVentaAnterior");
        var ordPrecioVentaNuevo = reader.GetOrdinal("precioVentaNuevo");
        var ordPrecioCostoAnterior = reader.GetOrdinal("precioCostoAnterior");
        var ordPrecioCostoNuevo = reader.GetOrdinal("precioCostoNuevo");
        var ordMotivoCambio = reader.GetOrdinal("motivoCambio");
        var ordIdUsuario = reader.GetOrdinal("idUsuario");
        var ordFechaCambio = reader.GetOrdinal("fechaCambio");

        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new ProductPriceHistory
            {
                Id = reader.GetInt32(ordId),
                IdProducto = reader.GetInt32(ordIdProducto),
                NombreProducto = reader.GetString(ordNombreProducto),
                PrecioVentaAnterior = reader.GetDecimal(ordPrecioVentaAnterior),
                PrecioVentaNuevo = reader.GetDecimal(ordPrecioVentaNuevo),
                PrecioCostoAnterior = reader.IsDBNull(ordPrecioCostoAnterior)
                    ? null
                    : reader.GetDecimal(ordPrecioCostoAnterior),
                PrecioCostoNuevo = reader.IsDBNull(ordPrecioCostoNuevo)
                    ? null
                    : reader.GetDecimal(ordPrecioCostoNuevo),
                MotivoCambio = reader.IsDBNull(ordMotivoCambio)
                    ? string.Empty
                    : reader.GetString(ordMotivoCambio),
                IdUsuario = reader.GetInt32(ordIdUsuario),
                FechaCambio = reader.GetDateTime(ordFechaCambio)
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
