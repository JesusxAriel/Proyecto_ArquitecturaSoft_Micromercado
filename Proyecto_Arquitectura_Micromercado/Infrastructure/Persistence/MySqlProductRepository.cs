using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using System.Data;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlProductRepository(IConfiguration configuration) : IProductRepository
{
    private const int SystemAdminId = 1;
    private readonly string connectionString = configuration.GetConnectionString("MySqlConnection")
        ?? throw new InvalidOperationException("No se configuró la conexión MySqlConnection.");

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
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new ProductListItem
            {
                Id = reader.GetInt32("id"),
                Nombre = reader.GetString("nombre"),
                EmpaquePresentacion = reader.GetString("empaquePresentacion"),
                PrecioVenta = reader.GetDecimal("precioVenta"),
                PrecioCosto = reader.GetDecimal("precioCosto"),
                StockMinimo = reader.GetInt32("stockMinimo"),
                IdCategoria = reader.GetInt32("idCategoria"),
                NombreCategoria = reader.GetString("nombreCategoria"),
                IdProveedor = reader.GetInt32("idProveedor"),
                NombreProveedor = reader.GetString("nombreProveedor"),
                StockCalculado = reader.GetInt32("stockCalculado")
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
            Id = reader.GetInt32("id"),
            Nombre = reader.GetString("nombre"),
            EmpaquePresentacion = reader.GetString("empaquePresentacion"),
            PrecioVenta = reader.GetDecimal("precioVenta"),
            PrecioCosto = reader.GetDecimal("precioCosto"),
            StockMinimo = reader.GetInt32("stockMinimo"),
            IdCategoria = reader.GetInt32("idCategoria"),
            IdProveedor = reader.GetInt32("idProveedor"),
            EstaActivo = reader.GetBoolean("estaActivo")
        };
    }

    public Task<IReadOnlyList<LookupOption>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombre FROM CATEGORIAS WHERE estaActivo = 1 ORDER BY nombre;", cancellationToken);

    public Task<IReadOnlyList<LookupOption>> GetSuppliersAsync(CancellationToken cancellationToken = default) =>
        GetLookupAsync("SELECT id, nombreEmpresa FROM PROVEEDOR WHERE estaActivo = 1 ORDER BY nombreEmpresa;", cancellationToken);

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
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        const string sql = """
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
        await using var command = new MySqlCommand(sql, connection);
        AddProductParameters(command, product);
        command.Parameters.AddWithValue("@id", product.Id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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
        var connection = new MySqlConnection(connectionString);
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
