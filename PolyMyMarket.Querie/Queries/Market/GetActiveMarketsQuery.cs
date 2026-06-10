using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get active markets
/// </summary>
public class GetActiveMarketsQuery : IRequest<List<Models.Market>>
{
}

/// <summary>
/// Handler for GetActiveMarketsQuery
/// </summary>
public class GetActiveMarketsQueryHandler : IRequestHandler<GetActiveMarketsQuery, List<Models.Market>>
{
    private readonly MarketContext _context;

    public GetActiveMarketsQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<List<Models.Market>> Handle(GetActiveMarketsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _context.Markets
            .Where(m => m.EndDate > now && m.Status == MarketStatus.Active)
            .OrderByDescending(m => m.InitialLiquidity)
            .ToListAsync(cancellationToken);
    }
}
