using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get user position for a specific market
/// </summary>
public class GetUserPositionForMarketQuery : IRequest<Position?>
{
    public int UserId { get; set; }
    public int MarketId { get; set; }

    public GetUserPositionForMarketQuery(int userId, int marketId)
    {
        UserId = userId;
        MarketId = marketId;
    }
}

/// <summary>
/// Handler for GetUserPositionForMarketQuery
/// </summary>
public class GetUserPositionForMarketQueryHandler : IRequestHandler<GetUserPositionForMarketQuery, Position?>
{
    private readonly MarketContext _context;

    public GetUserPositionForMarketQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Position?> Handle(GetUserPositionForMarketQuery request, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .Where(p => p.UserId == request.UserId && p.MarketId == request.MarketId)
            .Include(p => p.Market)
            .FirstOrDefaultAsync(cancellationToken);

        return position;
    }
}
