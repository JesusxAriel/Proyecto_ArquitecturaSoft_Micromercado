namespace Proyecto_Arquitectura_Micromercado.Domain.Common;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0
        ? 1
        : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public int FirstItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemNumber => TotalCount == 0 ? 0 : FirstItemNumber + Items.Count - 1;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class PageSizes
{
    public static readonly IReadOnlyList<int> Allowed = [5, 10, 20, 50];
    public const int Default = 10;

    public static int Normalize(int pageSize) =>
        Allowed.Contains(pageSize) ? pageSize : Default;
}
