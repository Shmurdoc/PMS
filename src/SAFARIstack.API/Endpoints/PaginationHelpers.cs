using Microsoft.EntityFrameworkCore;

namespace SAFARIstack.API.Endpoints;

/// <summary>
/// Paginated result wrapper for list endpoints.
/// </summary>
public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public static class PaginationHelpers
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Server-side pagination — executes COUNT and page query on the database.
    /// Use this for IQueryable sources (EF Core queries) to avoid loading entire tables.
    /// </summary>
    public static async Task<PaginatedResponse<T>> PaginateAsync<T>(
        IQueryable<T> source,
        int page = DefaultPage,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await source.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginatedResponse<T>(items, totalCount, page, pageSize, totalPages);
    }

    /// <summary>
    /// In-memory pagination — only for pre-materialized collections that are already bounded.
    /// Prefer PaginateAsync for any database-backed query.
    /// </summary>
    public static PaginatedResponse<T> Paginate<T>(
        IEnumerable<T> source,
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var items = source.ToList();
        var totalCount = items.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paged = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResponse<T>(paged, totalCount, page, pageSize, totalPages);
    }
}
