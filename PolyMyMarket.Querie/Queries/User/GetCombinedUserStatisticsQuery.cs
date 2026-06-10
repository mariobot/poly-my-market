using MediatR;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get combined user statistics (binary + multi-outcome)
/// </summary>
public class GetCombinedUserStatisticsQuery : IRequest<UserStatistics>
{
    public int UserId { get; set; }

    public GetCombinedUserStatisticsQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// Handler for GetCombinedUserStatisticsQuery
/// </summary>
public class GetCombinedUserStatisticsQueryHandler : IRequestHandler<GetCombinedUserStatisticsQuery, UserStatistics>
{
    private readonly IMediator _mediator;

    public GetCombinedUserStatisticsQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<UserStatistics> Handle(GetCombinedUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Binary positions
        var binaryPositions = await _mediator.Send(new GetUserPositionsQuery(request.UserId), cancellationToken);
        decimal binaryInvested = binaryPositions.Sum(p => p.Position.TotalInvested);
        decimal binaryValue = binaryPositions.Sum(p => p.CurrentValue);

        // Multi-outcome positions
        var outcomePositions = await _mediator.Send(new GetUserOutcomePositionsQuery(request.UserId), cancellationToken);
        decimal outcomeInvested = outcomePositions.Sum(p => p.Position.TotalInvested);
        decimal outcomeValue = outcomePositions.Sum(p => p.CurrentValue);

        // Combined
        decimal totalInvested = binaryInvested + outcomeInvested;
        decimal currentValue = binaryValue + outcomeValue;
        decimal totalProfitLoss = currentValue - totalInvested;

        var orders = await _mediator.Send(new GetUserOrdersQuery(request.UserId), cancellationToken);

        var userQuery = new GetUserByIdQuery(request.UserId);
        var user = await _mediator.Send(userQuery, cancellationToken);

        return new UserStatistics
        {
            Balance = user?.Balance ?? 0,
            TotalInvested = totalInvested,
            CurrentPortfolioValue = currentValue,
            TotalProfitLoss = totalProfitLoss,
            TotalOrders = orders.Count,
            ActivePositions = binaryPositions.Count + outcomePositions.Count,
            MemberSince = user?.CreatedDate ?? DateTime.MinValue
        };
    }
}
