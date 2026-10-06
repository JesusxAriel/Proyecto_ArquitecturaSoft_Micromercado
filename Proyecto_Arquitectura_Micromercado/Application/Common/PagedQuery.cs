using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Application.Common;

public static class PagedQuery
{
    // Normaliza página y tamaño; si la página pedida quedó fuera de rango (p. ej. se eliminaron
    // registros) devuelve la última.
    public static async Task<PagedResult<T>> ExecuteAsync<T>(
        int page,
        int pageSize,
        Func<int, int, Task<PagedResult<T>>> fetch)
    {
        pageSize = PageSizes.Normalize(pageSize);
        page = Math.Max(1, page);

        var result = await fetch(page, pageSize);

        if (result.Items.Count == 0 && result.TotalCount > 0 && page > result.TotalPages)
        {
            result = await fetch(result.TotalPages, pageSize);
        }

        return result;
    }

    public static string? NormalizeSearch(string? search) =>
        string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
