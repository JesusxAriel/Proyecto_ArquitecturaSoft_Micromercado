using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlProductRepository : IProductRepository
{
    private const int SystemAdminId = 1;
    private readonly IPriceHistoryRepository _priceHistoryRepository = new MySqlPriceHistoryRepository();

    public async Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, empaquePresentacion, precioVenta, precioCosto,
                   stockMinimo, idCategoria, nombreCategoria, idProveedor,
                   nombreProveedor, stockCalculado
            FROM vw_productos_con_stock
            ORDER BY nombre;
            """;

        var products = new List<ProductListItem>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(
            System.Data.CommandBehavior.SequentialAccess,
            cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ProductListItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                EmpaquePresentacion = reader.GetString(reader.GetOrdinal("empaquePresentacion")),
                PrecioVenta = reader.GetDecimal(reader.GetOrdinal("precioVenta")),
                PrecioCosto = reader.GetDecimal(reader.GetOrdinal("precioCosto")),
                StockMinimo = reader.GetInt32(reader.GetOrdinal("stockMinimo")),
                IdCategoria = reader.GetInt32(reader.GetOrdinal("idCategoria")),
                NombreCategoria = reader.GetString(reader.GetOrdinal("nombreCategoria")),
                IdProveedor = reader.GetInt32(reader.GetOrdinal("idProveedor")),
                NombreProveedor = reader.GetString(reader.GetOrdinal("nombreProveedor")),
                StockCalculado = reader.GetInt32(reader.GetOrdinal("stockCalculado"))
            });
        }

        return products;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre, empaquePresentacion, precioVenta, precioCosto,
                   stockMinimo, idCategoria, idProveedor, estaActivo
            FROM PRODUCTO
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new Product
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            EmpaquePresentacion = reader.GetString(reader.GetOrdinal("empaquePresentacion")),
            PrecioVenta = reader.GetDecimal(reader.GetOrdinal("precioVenta")),
            PrecioCosto = reader.GetDecimal(reader.GetOrdinal("precioCosto")),
            StockMinimo = reader.GetInt32(reader.GetOrdinal("stockMinimo")),
            IdCategoria = reader.GetInt32(reader.GetOrdinal("idCategoria")),
            IdProveedor = reader.GetInt32(reader.GetOrdinal("idProveedor")),
            EstaActivo = reader.GetBoolean(reader.GetOrdinal("estaActivo"))
        };
    }

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombre FROM CATEGORIAS WHERE estaActivo = 1 ORDER BY nombre;", cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombreEmpresa FROM PROVEEDOR WHERE estaActivo = 1 ORDER BY nombreEmpresa;", cancellationToken);

    public async Task<IReadOnlyList<ProductPriceHistory>> GetPriceHistoryAsync(
        CancellationToken cancellationToken = default)
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

    public async Task AddPriceHistoryAsync(
        ProductPriceHistory history,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);
        await _priceHistoryRepository.AddPriceHistoryAsync(history, cancellationToken);
    }

    public async Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO PRODUCTO
                (nombre, empaquePresentacion, precioVenta, precioCosto, stockMinimo,
                 idCategoria, idProveedor, estaActivo, idUsuarioAdmin)
            VALUES
                (@nombre, @empaquePresentacion, @precioVenta, @precioCosto, @stockMinimo,
                 @idCategoria, @idProveedor, 1, @idUsuarioAdmin);
            SELECT LAST_INSERT_ID();
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        AddProductParameters(command, product);
        command.Parameters.AddWithValue("@idUsuarioAdmin", SystemAdminId);
        var generatedId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        product.Id = generatedId;
        return generatedId;
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string selectSql = """
            SELECT precioVenta, precioCosto
            FROM PRODUCTO
            WHERE id = @id AND estaActivo = 1
            FOR UPDATE;
            """;
        const string updateSql = """
            UPDATE PRODUCTO
            SET nombre = @nombre,
                empaquePresentacion = @empaquePresentacion,
                precioVenta = @precioVenta,
                precioCosto = @precioCosto,
                stockMinimo = @stockMinimo,
                idCategoria = @idCategoria,
                idProveedor = @idProveedor
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        decimal precioVentaAnterior;
        decimal precioCostoAnterior;
        await using (var selectCommand = new MySqlCommand(selectSql, connection, transaction))
        {
            selectCommand.Parameters.AddWithValue("@id", product.Id);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            precioVentaAnterior = reader.GetDecimal(reader.GetOrdinal("precioVenta"));
            precioCostoAnterior = reader.GetDecimal(reader.GetOrdinal("precioCosto"));
        }

        int affectedRows;
        await using (var updateCommand = new MySqlCommand(updateSql, connection, transaction))
        {
            AddProductParameters(updateCommand, product);
            updateCommand.Parameters.AddWithValue("@id", product.Id);
            affectedRows = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (affectedRows == 1 &&
            (precioVentaAnterior != product.PrecioVenta || precioCostoAnterior != product.PrecioCosto))
        {
            var history = new ProductPriceHistory
            {
                IdProducto = product.Id,
                PrecioVentaAnterior = precioVentaAnterior,
                PrecioVentaNuevo = product.PrecioVenta,
                PrecioCostoAnterior = precioCostoAnterior,
                PrecioCostoNuevo = product.PrecioCosto,
                MotivoCambio = string.IsNullOrWhiteSpace(product.MotivoCambio)
                    ? "Actualización de precio"
                    : product.MotivoCambio.Trim(),
                IdUsuario = SystemAdminId
            };

            await _priceHistoryRepository.AddPriceHistoryAsync(connection, transaction, history, cancellationToken);
        }

        if (affectedRows == 1)
        {
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await transaction.RollbackAsync(cancellationToken);
        }

        return affectedRows == 1;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE PRODUCTO
            SET estaActivo = 0
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private async Task<IReadOnlyList<LookupOption>> GetLookupAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        var options = new List<LookupOption>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new LookupOption(reader.GetInt32(0), reader.GetString(1)));
        }

        return options;
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = DatabaseConnection.Instance.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddProductParameters(MySqlCommand command, Product product)
    {
        command.Parameters.AddWithValue("@nombre", product.Nombre.Trim());
        command.Parameters.AddWithValue("@empaquePresentacion", product.EmpaquePresentacion.Trim());
        command.Parameters.AddWithValue("@precioVenta", product.PrecioVenta);
        command.Parameters.AddWithValue("@precioCosto", product.PrecioCosto);
        command.Parameters.AddWithValue("@stockMinimo", product.StockMinimo);
        command.Parameters.AddWithValue("@idCategoria", product.IdCategoria);
        command.Parameters.AddWithValue("@idProveedor", product.IdProveedor);
    }
}
