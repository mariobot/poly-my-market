using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get market with outcomes (multi-outcome market)
/// </summary>
public class GetMarketWithOutcomesQuery : IRequest<Models.Market?>
{
    public int MarketId { get; set; }

    public GetMarketWithOutcomesQuery(int marketId)
    {
        MarketId = marketId;
    }
}

/// <summary>
/// Handler for GetMarketWithOutcomesQuery
/// </summary>
public class GetMarketWithOutcomesQueryHandler : IRequestHandler<GetMarketWithOutcomesQuery, Models.Market?>
{
    private readonly MarketContext _context;

    public GetMarketWithOutcomesQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Models.Market?> Handle(GetMarketWithOutcomesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Markets
            .Where(m => m.Id == request.MarketId)
            .Include(m => m.Outcomes)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
