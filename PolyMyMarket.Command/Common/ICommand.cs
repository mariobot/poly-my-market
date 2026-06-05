namespace PolyMyMarket.Command.Common;

/// <summary>
/// Base interface for all commands in the system
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Command that returns a result
/// </summary>
public interface ICommand<TResult> : ICommand
{
}
