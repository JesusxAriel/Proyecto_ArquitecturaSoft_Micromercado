using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using System.Data;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlSupplierRepository(IConfiguration configuration) : ISupplierRepository
{
    private const int SystemAdminId = 1;
    private readonly string connectionString = configuration.GetConnectionString("MySqlConnection")
        ?? throw new InvalidOperationException("No se configuró la conexión MySqlConnection.");

    public async Task<IReadOnlyList<SupplierListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombreEmpresa, numeroEmpresa, correoReferencia, esAutogestionado
            FROM PROVEEDOR
            WHERE estaActivo = 1
            ORDER BY nombreEmpresa;
            """;

        var suppliers = new List<SupplierListItem>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            suppliers.Add(new SupplierListItem
            {
                Id = reader.GetInt32("id"),
                NombreEmpresa = reader.GetString("nombreEmpresa"),
                NumeroEmpresa = reader.GetString("numeroEmpresa"),
                CorreoReferencia = reader.IsDBNull("correoReferencia")
                    ? string.Empty
                    : reader.GetString("correoReferencia"),
                EsAutogestionado = reader.GetBoolean("esAutogestionado")
            });
        }

        return suppliers;
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombreEmpresa, numeroEmpresa, correoReferencia,
                   esAutogestionado, estaActivo
            FROM PROVEEDOR
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

        return new Supplier
        {
            Id = reader.GetInt32("id"),
            NombreEmpresa = reader.GetString("nombreEmpresa"),
            NumeroEmpresa = reader.GetString("numeroEmpresa"),
            CorreoReferencia = reader.IsDBNull("correoReferencia")
                ? null
                : reader.GetString("correoReferencia"),
            EsAutogestionado = reader.GetBoolean("esAutogestionado"),
            EstaActivo = reader.GetBoolean("estaActivo")
        };
    }

    public async Task<bool> ExistsNombreEmpresaAsync(
        string nombreEmpresa,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM PROVEEDOR
            WHERE nombreEmpresa = @nombreEmpresa
              AND id <> @idExcluido
              AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@nombreEmpresa", nombreEmpresa);
        command.Parameters.AddWithValue("@idExcluido", idExcluido);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<int> CreateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO PROVEEDOR
                (nombreEmpresa, numeroEmpresa, correoReferencia,
                 esAutogestionado, estaActivo, idUsuarioAdmin)
            VALUES
                (@nombreEmpresa, @numeroEmpresa, @correoReferencia,
                 @esAutogestionado, 1, @idUsuarioAdmin);
            SELECT LAST_INSERT_ID();
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        AddSupplierParameters(command, supplier);
        command.Parameters.AddWithValue("@idUsuarioAdmin", SystemAdminId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<bool> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE PROVEEDOR
            SET nombreEmpresa = @nombreEmpresa,
                numeroEmpresa = @numeroEmpresa,
                correoReferencia = @correoReferencia,
                esAutogestionado = @esAutogestionado
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        AddSupplierParameters(command, supplier);
        command.Parameters.AddWithValue("@id", supplier.Id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE PROVEEDOR
            SET estaActivo = 0
            WHERE id = @id AND estaActivo = 1;
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddSupplierParameters(MySqlCommand command, Supplier supplier)
    {
        command.Parameters.AddWithValue("@nombreEmpresa", supplier.NombreEmpresa.Trim());
        command.Parameters.AddWithValue("@numeroEmpresa", supplier.NumeroEmpresa.Trim());
        command.Parameters.AddWithValue("@correoReferencia", (object?)supplier.CorreoReferencia ?? DBNull.Value);
        command.Parameters.AddWithValue("@esAutogestionado", supplier.EsAutogestionado);
    }
}