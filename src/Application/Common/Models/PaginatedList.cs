namespace sp26se058_3dprintshop_be.Application.Common.Models;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public PaginationMetadata Metadata { get; }

    /* Code cũ của template
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    
    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
    */
    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        Metadata = new PaginationMetadata(count, pageNumber, pageSize);
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
public class PaginationMetadata 
{
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public int PageSize { get; init; }    // Cái này gửi về chỉ để đối chiếu là chủ yếu, không quan trọng
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public PaginationMetadata(int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }
}
