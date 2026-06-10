using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get user multi-outcome positions
/// </summary>
public class GetUserOutcomePositionsQuery : IRequest<List<OutcomePositionViewModel>>
{
    public int UserId { get; set; }

    public GetUserOutcomePositionsQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// View model for multi-outcome positions
/// </summary>
public class OutcomePositionViewModel
{
    public Models.OutcomePosition Position { get; set; } = null!;
    public Models.MarketOutcome MarketOutcome { get; set; } = null!;
    public Models.Market Market { get; set; } = null!;
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent =>
        Position.TotalInvested > 0
            ? (ProfitLoss / Position.TotalInvested) * 100
            : 0;
}

/// <summary>
/// Handler for GetUserOutcomePositionsQuery
/// </summary>
public class GetUserOutcomePositionsQueryHandler : IRequestHandler<GetUserOutcomePositionsQuery, List<OutcomePositionViewModel>>
{
    private readonly MarketContext _context;

    public GetUserOutcomePositionsQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<OutcomePositionViewModel>> Handle(GetUserOutcomePositionsQuery request, CancellationToken cancellationToken)
    {
        var positions = await _context.OutcomePositions
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.MarketOutcome)
            .ThenInclude(mo => mo.Market)
            .ToListAsync(cancellationToken);

        var viewModels = new List<OutcomePositionViewModel>();
        foreach (var position in positions)
        {
            var market = position.MarketOutcome.Market;

            // Calculate price for this outcome based on total liquidity across all outcomes
            decimal totalLiquidity = await _context.MarketOutcomes
                .Where(mo => mo.MarketId == market.Id)
                .SumAsync(mo => mo.LiquidityPool, cancellationToken);

            var outcomePrice = totalLiquidity > 0 ? position.MarketOutcome.LiquidityPool / totalLiquidity : 0;
            var currentValue = position.Shares * outcomePrice;
            var profitLoss = currentValue - position.TotalInvested;

            viewModels.Add(new OutcomePositionViewModel
            {
                Position = position,
                MarketOutcome = position.MarketOutcome,
                Market = market,
                CurrentPrice = outcomePrice,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss
            });
        }

        return viewModels;
    }
}
