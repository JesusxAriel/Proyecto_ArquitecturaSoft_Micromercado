namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

internal static class SqlLike
{
    public static object ContainsPattern(string? search) =>
        string.IsNullOrWhiteSpace(search)
            ? DBNull.Value
            : "%" + search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_") + "%";
}
