using MediatR;
using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Querie.Queries.Market;

/// <summary>
/// Query to get multi-outcome market prices
/// </summary>
public class GetMultiOutcomePricesQuery : IRequest<Dictionary<int, List<decimal>>>
{
    public int MarketId { get; set; }

    public GetMultiOutcomePricesQuery(int marketId)
    {
        MarketId = marketId;
    }
}

/// <summary>
/// Handler for GetMultiOutcomePricesQuery
/// </summary>
public class GetMultiOutcomePricesQueryHandler : IRequestHandler<GetMultiOutcomePricesQuery, Dictionary<int, List<decimal>>>
{
    private readonly MarketContext _context;

    public GetMultiOutcomePricesQueryHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, List<decimal>>> Handle(GetMultiOutcomePricesQuery request, CancellationToken cancellationToken)
    {
        var outcomes = await _context.MarketOutcomes
            .Where(mo => mo.MarketId == request.MarketId)
            .OrderBy(mo => mo.DisplayOrder)
            .ToListAsync(cancellationToken);

        var prices = new Dictionary<int, List<decimal>>();

        if (outcomes.Any())
        {
            var outcomePrices = new List<decimal>();
            decimal totalLiquidity = outcomes.Sum(o => o.LiquidityPool);

            if (totalLiquidity > 0)
            {
                foreach (var outcome in outcomes)
                {
                    outcomePrices.Add(outcome.LiquidityPool / totalLiquidity);
                }
            }
            else
            {
                // Equal distribution if no liquidity
                decimal equalPrice = 1m / outcomes.Count;
                outcomePrices = Enumerable.Repeat(equalPrice, outcomes.Count).ToList();
            }

            prices[request.MarketId] = outcomePrices;
        }

        return prices;
    }
}
