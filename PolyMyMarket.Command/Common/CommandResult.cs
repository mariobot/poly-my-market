namespace PolyMyMarket.Command.Common;

/// <summary>
/// Standard result type for commands
/// </summary>
public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static CommandResult SuccessResult(string message = "Operation completed successfully", object? data = null)
    {
        return new CommandResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static CommandResult FailureResult(string message)
    {
        return new CommandResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Generic result type for commands that return typed data
/// </summary>
public class CommandResult<T> : CommandResult
{
    public new T? Data { get; set; }

    public static CommandResult<T> SuccessResult(string message, T data)
    {
        return new CommandResult<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static new CommandResult<T> FailureResult(string message)
    {
        return new CommandResult<T>
        {
            Success = false,
            Message = message
        };
    }
}
