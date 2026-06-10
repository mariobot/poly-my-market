using MediatR;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get user statistics
/// </summary>
public class GetUserStatisticsQuery : IRequest<UserStatistics>
{
    public int UserId { get; set; }

    public GetUserStatisticsQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// User statistics model
/// </summary>
public class UserStatistics
{
    public decimal Balance { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentPortfolioValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public int TotalOrders { get; set; }
    public int ActivePositions { get; set; }
    public DateTime MemberSince { get; set; }
}

/// <summary>
/// Handler for GetUserStatisticsQuery
/// </summary>
public class GetUserStatisticsQueryHandler : IRequestHandler<GetUserStatisticsQuery, UserStatistics>
{
    private readonly MarketContext _context;
    private readonly IMediator _mediator;

    public GetUserStatisticsQueryHandler(MarketContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<UserStatistics> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user == null)
            return new UserStatistics();

        var positions = await _mediator.Send(new GetUserPositionsQuery(request.UserId), cancellationToken);
        var orders = await _mediator.Send(new GetUserOrdersQuery(request.UserId), cancellationToken);

        decimal totalInvested = positions.Sum(p => p.Position.TotalInvested);
        decimal currentValue = positions.Sum(p => p.CurrentValue);
        decimal totalProfitLoss = currentValue - totalInvested;

        return new UserStatistics
        {
            Balance = user.Balance,
            TotalInvested = totalInvested,
            CurrentPortfolioValue = currentValue,
            TotalProfitLoss = totalProfitLoss,
            TotalOrders = orders.Count,
            ActivePositions = positions.Count,
            MemberSince = user.CreatedDate
        };
    }
}
