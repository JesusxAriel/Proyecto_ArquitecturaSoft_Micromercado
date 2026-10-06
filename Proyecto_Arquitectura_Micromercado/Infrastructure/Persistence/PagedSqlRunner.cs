using System.Data.Common;
using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

// Ejecuta COUNT + página (LIMIT/OFFSET) con filtro de búsqueda opcional. Lo comparten los
// repositorios de productos, categorías y proveedores.
internal static class PagedSqlRunner
{
    public static async Task<PagedResult<T>> RunAsync<T>(
        MySqlConnection connection,
        string columns,
        string from,
        string? baseWhere,
        string searchCondition,
        string orderBy,
        string? search,
        int page,
        int pageSize,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(baseWhere))
        {
            conditions.Add(baseWhere);
        }

        conditions.Add($"(@search IS NULL OR {searchCondition})");

        var where = "WHERE " + string.Join(" AND ", conditions);
        var countSql = $"SELECT COUNT(*) FROM {from} {where};";
        var pageSql = $"SELECT {columns} FROM {from} {where} ORDER BY {orderBy} LIMIT @limit OFFSET @offset;";
        var searchPattern = SqlLike.ContainsPattern(search);

        int totalCount;
        await using (var countCommand = new MySqlCommand(countSql, connection))
        {
            countCommand.Parameters.AddWithValue("@search", searchPattern);
            totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
        }

        var items = new List<T>();
        await using (var pageCommand = new MySqlCommand(pageSql, connection))
        {
            pageCommand.Parameters.AddWithValue("@search", searchPattern);
            pageCommand.Parameters.AddWithValue("@limit", pageSize);
            pageCommand.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            await using var reader = await pageCommand.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(map(reader));
            }
        }

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
