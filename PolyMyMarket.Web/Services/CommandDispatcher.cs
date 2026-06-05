using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Web.Services;

/// <summary>
/// Service to simplify command execution by resolving handlers from DI
/// </summary>
public class CommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Execute a command that returns a result
    /// </summary>
    public async Task<TResult> ExecuteAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    /// <summary>
    /// Execute a command without result
    /// </summary>
    public async Task<CommandResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<CommandResult>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, CommandResult>>();
        return await handler.HandleAsync(command, cancellationToken);
    }
}
