using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.User;

/// <summary>
/// Query to get user orders
/// </summary>
public class GetUserOrdersQuery : IRequest<List<Order>>
{
    public int UserId { get; set; }
    public int? MarketId { get; set; }
    public int Count { get; set; } = 50;

    public GetUserOrdersQuery(int userId, int? marketId = null, int count = 50)
    {
        UserId = userId;
        MarketId = marketId;
        Count = count;
    }
}

/// <summary>
/// Handler for GetUserOrdersQuery
/// </summary>
public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, List<Order>>
{
    private readonly MarketContext _context;

    public GetUserOrdersQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _context.Orders
            .Where(o => o.UserId == request.UserId)
            .Include(o => o.Market);

        if (request.MarketId.HasValue)
        {
            query = query.Where(o => o.MarketId == request.MarketId.Value);
        }

        return await query
            .OrderByDescending(o => o.Timestamp)
            .Take(request.Count)
            .ToListAsync(cancellationToken);
    }
}
