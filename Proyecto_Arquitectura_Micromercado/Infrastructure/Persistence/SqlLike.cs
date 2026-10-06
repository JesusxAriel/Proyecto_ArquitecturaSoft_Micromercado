namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

internal static class SqlLike
{
    // En LIKE el backslash es el escape por defecto de MySQL: se escapan \, % y _ del texto buscado.
    // Devuelve NULL cuando no hay búsqueda (la consulta usa "@search IS NULL").
    public static object ContainsPattern(string? search) =>
        string.IsNullOrWhiteSpace(search)
            ? DBNull.Value
            : "%" + search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_") + "%";
}
