using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get recent orders for a market
/// </summary>
public class GetRecentOrdersQuery : IRequest<List<Order>>
{
    public int MarketId { get; set; }
    public int Count { get; set; } = 20;

    public GetRecentOrdersQuery(int marketId, int count = 20)
    {
        MarketId = marketId;
        Count = count;
    }
}

/// <summary>
/// Handler for GetRecentOrdersQuery
/// </summary>
public class GetRecentOrdersQueryHandler : IRequestHandler<GetRecentOrdersQuery, List<Order>>
{
    private readonly MarketContext _context;

    public GetRecentOrdersQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> Handle(GetRecentOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Where(o => o.MarketId == request.MarketId)
            .Include(o => o.User)
            .OrderByDescending(o => o.Timestamp)
            .Take(request.Count)
            .ToListAsync(cancellationToken);
    }
}
