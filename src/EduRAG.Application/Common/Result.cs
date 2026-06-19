namespace EduRAG.Application.Common;

public class Result<T>
{
    public bool    IsSuccess { get; }
    public T?      Value     { get; }
    public string? Error     { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value     = value;
        Error     = error;
    }

    public static Result<T> Success(T value)      => new(true,  value,   null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

public class PaginatedList<T>
{
    public List<T> Items      { get; }
    public int     TotalCount { get; }
    public int     Page       { get; }
    public int     PageSize   { get; }
    public int     TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PaginatedList(List<T> items, int totalCount, int page, int pageSize)
    {
        Items      = items;
        TotalCount = totalCount;
        Page       = page;
        PageSize   = pageSize;
    }
}
