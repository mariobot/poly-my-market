using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get current market prices
/// </summary>
public class GetCurrentMarketPricesQuery : IRequest<Dictionary<int, decimal>>
{
    public List<int> MarketIds { get; set; }

    public GetCurrentMarketPricesQuery(List<int> marketIds)
    {
        MarketIds = marketIds;
    }
}

/// <summary>
/// Handler for GetCurrentMarketPricesQuery
/// </summary>
public class GetCurrentMarketPricesQueryHandler : IRequestHandler<GetCurrentMarketPricesQuery, Dictionary<int, decimal>>
{
    private readonly MarketContext _context;

    public GetCurrentMarketPricesQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, decimal>> Handle(GetCurrentMarketPricesQuery request, CancellationToken cancellationToken)
    {
        var markets = await _context.Markets
            .Where(m => request.MarketIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var prices = new Dictionary<int, decimal>();
        foreach (var market in markets)
        {
            var yesPrice = CalculatePrice(market.YesPool, market.NoPool);
            prices[market.Id] = yesPrice;
        }

        return prices;
    }

    private decimal CalculatePrice(decimal yesPool, decimal noPool)
    {
        if (yesPool + noPool == 0)
            return 0.5m;

        return yesPool / (yesPool + noPool);
    }
}
