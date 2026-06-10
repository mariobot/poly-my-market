namespace PolyMyMarket.Querie.Common;

/// <summary>
/// Standard result type for queries
/// </summary>
public class QueryResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static QueryResult SuccessResult(string message = "Query completed successfully", object? data = null)
    {
        return new QueryResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static QueryResult FailureResult(string message)
    {
        return new QueryResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Generic result type for queries that return typed data
/// </summary>
public class QueryResult<T> : QueryResult
{
    public new T? Data { get; set; }

    public static QueryResult<T> SuccessResult(string message, T data)
    {
        return new QueryResult<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static new QueryResult<T> FailureResult(string message)
    {
        return new QueryResult<T>
        {
            Success = false,
            Message = message
        };
    }
}
