using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get user positions
/// </summary>
public class GetUserPositionsQuery : IRequest<List<PositionViewModel>>
{
    public int UserId { get; set; }

    public GetUserPositionsQuery(int userId)
    {
        UserId = userId;
    }
}

/// <summary>
/// View model for positions with calculated values
/// </summary>
public class PositionViewModel
{
    public Models.Position Position { get; set; } = null!;
    public Models.Market Market { get; set; } = null!;
    public decimal CurrentYesPrice { get; set; }
    public decimal CurrentNoPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercent =>
        Position.TotalInvested > 0
            ? (ProfitLoss / Position.TotalInvested) * 100
            : 0;
}

/// <summary>
/// Handler for GetUserPositionsQuery
/// </summary>
public class GetUserPositionsQueryHandler : IRequestHandler<GetUserPositionsQuery, List<PositionViewModel>>
{
    private readonly MarketContext _context;

    public GetUserPositionsQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<PositionViewModel>> Handle(GetUserPositionsQuery request, CancellationToken cancellationToken)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == request.UserId)
            .Include(p => p.Market)
            .ToListAsync(cancellationToken);

        var viewModels = new List<PositionViewModel>();
        foreach (var position in positions)
        {
            var yesPrice = CalculateYesPrice(position.Market.YesPool, position.Market.NoPool);
            var currentValue = position.YesShares * yesPrice + position.NoShares * (1 - yesPrice);
            var profitLoss = currentValue - position.TotalInvested;

            viewModels.Add(new PositionViewModel
            {
                Position = position,
                Market = position.Market,
                CurrentYesPrice = yesPrice,
                CurrentNoPrice = 1 - yesPrice,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss
            });
        }

        return viewModels;
    }

    private decimal CalculateYesPrice(decimal yesPool, decimal noPool)
    {
        if ((yesPool + noPool) == 0)
            return 0.5m;

        return yesPool / (yesPool + noPool);
    }
}
