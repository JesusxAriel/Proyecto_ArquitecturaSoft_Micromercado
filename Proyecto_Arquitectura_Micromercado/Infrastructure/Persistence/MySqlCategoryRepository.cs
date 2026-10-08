using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Common;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlCategoryRepository(DatabaseConnection conexion) : ICategoryRepository
{
    private const int SystemAdminId = 1;
    private const int DuplicateKeyErrorNumber = 1062;

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id,
                   nombre,
                   descripcion,
                   codigo,
                   pasilloUbicacion,
                   estaActivo,
                   idUsuarioAdmin,
                   fechaCreacion,
                   fechaActualizacion
            FROM CATEGORIAS
            WHERE estaActivo = 1
            ORDER BY nombre ASC;
            """;

        var categories = new List<Category>();

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(MapCategory(reader));
        }

        return categories;
    }

    public async Task<PagedResult<Category>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        return await PagedSqlRunner.RunAsync(
            connection,
            columns: """
                id, nombre, descripcion, codigo, pasilloUbicacion, estaActivo,
                idUsuarioAdmin, fechaCreacion, fechaActualizacion
                """,
            from: "CATEGORIAS",
            baseWhere: "estaActivo = 1",
            searchCondition: """
                nombre LIKE @search
                OR codigo LIKE @search
                OR pasilloUbicacion LIKE @search
                OR descripcion LIKE @search
                """,
            orderBy: "nombre, id",
            search,
            page,
            pageSize,
            MapCategory,
            cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id,
                   nombre,
                   descripcion,
                   codigo,
                   pasilloUbicacion,
                   estaActivo,
                   idUsuarioAdmin,
                   fechaCreacion,
                   fechaActualizacion
            FROM CATEGORIAS
            WHERE id = @id
              AND estaActivo = 1;
            """;

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@id", id);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapCategory(reader);
    }

    public async Task<int> CreateAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO CATEGORIAS
                (nombre, descripcion, codigo, pasilloUbicacion, idUsuarioAdmin)
            VALUES
                (@nombre, @descripcion, @codigo, @pasilloUbicacion, @idUsuarioAdmin);
            SELECT LAST_INSERT_ID();
            """;

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        AddCategoryParameters(command, category);

        int generatedId;

        try
        {
            generatedId =
                Convert.ToInt32(
                    await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (MySqlException ex) when (ex.Number == DuplicateKeyErrorNumber)
        {
            throw ToDuplicateException(ex);
        }

        category.Id = generatedId;

        return generatedId;
    }

    public async Task<bool> UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE CATEGORIAS
            SET nombre = @nombre,
                descripcion = @descripcion,
                codigo = @codigo,
                pasilloUbicacion = @pasilloUbicacion,
                idUsuarioAdmin = @idUsuarioAdmin
            WHERE id = @id
              AND estaActivo = 1;
            """;

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        AddCategoryParameters(command, category);

        command.Parameters.AddWithValue(
            "@id",
            category.Id);

        try
        {
            int affectedRows =
                await command.ExecuteNonQueryAsync(cancellationToken);

            return affectedRows == 1;
        }
        catch (MySqlException ex) when (ex.Number == DuplicateKeyErrorNumber)
        {
            throw ToDuplicateException(ex);
        }
    }

    public async Task<bool> ExistsNameAsync(
        string name,
        int idExcluido,
        CancellationToken cancellationToken = default)
    {
        // La collation utf8mb4_unicode_ci ignora mayúsculas y tildes; REGEXP_REPLACE los espacios extra.
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM CATEGORIAS
                WHERE estaActivo = 1
                  AND id <> @idExcluido
                  AND TRIM(REGEXP_REPLACE(nombre, '[[:space:]]+', ' ')) = @nombre
            );
            """;

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue("@nombre", name.Trim());
        command.Parameters.AddWithValue("@idExcluido", idExcluido);

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<IReadOnlyList<string>> GetAllCodesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT codigo FROM CATEGORIAS;";

        var codes = new List<string>();

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            codes.Add(reader.GetString(0));
        }

        return codes;
    }

    public async Task<bool> SoftDeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE CATEGORIAS
            SET estaActivo = 0,
                idUsuarioAdmin = @idUsuarioAdmin
            WHERE id = @id
              AND estaActivo = 1;
            """;

        await using var connection =
            await OpenConnectionAsync(cancellationToken);

        await using var command =
            new MySqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@id",
            id);

        command.Parameters.AddWithValue(
            "@idUsuarioAdmin",
            SystemAdminId);

        return await command.ExecuteNonQueryAsync(
            cancellationToken) == 1;
    }

    // El mensaje de MySQL indica qué índice UNIQUE se violó.
    private static ArgumentException ToDuplicateException(MySqlException ex) =>
        ex.Message.Contains("UQ_Categorias_nombre_activo", StringComparison.OrdinalIgnoreCase)
            ? new DuplicateCategoryNameException(ex)
            : new DuplicateCategoryCodeException(ex);

    private async Task<MySqlConnection> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection =
            conexion.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    private static void AddCategoryParameters(
        MySqlCommand command,
        Category category)
    {
        command.Parameters.AddWithValue(
            "@nombre",
            category.Name.Trim());

        command.Parameters.AddWithValue(
            "@descripcion",
            (object?)category.Description ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@codigo",
            category.Code.Trim());

        command.Parameters.AddWithValue(
            "@pasilloUbicacion",
            (object?)category.AisleLocation ?? DBNull.Value);

        command.Parameters.AddWithValue(
            "@idUsuarioAdmin",
            SystemAdminId);
    }

    private static Category MapCategory(
        System.Data.Common.DbDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(
                reader.GetOrdinal("id")),

            Name = reader.GetString(
                reader.GetOrdinal("nombre")),

            Description =
                reader.IsDBNull(
                    reader.GetOrdinal("descripcion"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("descripcion")),

            Code = reader.GetString(
                reader.GetOrdinal("codigo")),

            AisleLocation =
                reader.IsDBNull(
                    reader.GetOrdinal("pasilloUbicacion"))
                    ? null
                    : reader.GetString(
                        reader.GetOrdinal("pasilloUbicacion")),

            IsActive =
                reader.GetBoolean(
                    reader.GetOrdinal("estaActivo")),

            AdminUserId =
                reader.GetInt32(
                    reader.GetOrdinal("idUsuarioAdmin")),

            CreatedAt =
                reader.GetDateTime(
                    reader.GetOrdinal("fechaCreacion")),

            UpdatedAt =
                reader.IsDBNull(
                    reader.GetOrdinal("fechaActualizacion"))
                    ? null
                    : reader.GetDateTime(
                        reader.GetOrdinal("fechaActualizacion"))
        };
    }
}