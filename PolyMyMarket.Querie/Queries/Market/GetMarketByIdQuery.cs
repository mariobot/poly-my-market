using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get market by ID
/// </summary>
public class GetMarketByIdQuery : IRequest<Models.Market?>
{
    public int MarketId { get; set; }

    public GetMarketByIdQuery(int marketId)
    {
        MarketId = marketId;
    }
}

/// <summary>
/// Handler for GetMarketByIdQuery
/// </summary>
public class GetMarketByIdQueryHandler : IRequestHandler<GetMarketByIdQuery, Models.Market?>
{
    private readonly MarketContext _context;

    public GetMarketByIdQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Models.Market?> Handle(GetMarketByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Markets
            .FirstOrDefaultAsync(m => m.Id == request.MarketId, cancellationToken);
    }
}
