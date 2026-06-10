using MediatR;

namespace PolyMyMarket.Web.Services;

/// <summary>
/// Service to simplify query execution using MediatR
/// </summary>
public class QueryDispatcher
{
    private readonly IMediator _mediator;

    public QueryDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Execute a query
    /// </summary>
    public async Task<TResponse> ExecuteAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IRequest<TResponse>
    {
        return await _mediator.Send(query, cancellationToken);
    }
}
