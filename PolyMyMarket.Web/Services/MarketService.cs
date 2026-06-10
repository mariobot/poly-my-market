using PolyMyMarket.Context;
using PolyMyMarket.Models;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Command.Market;
using PolyMyMarket.Querie.Queries.Market;

namespace PolyMyMarket.Web.Services;

public class MarketService
{
    private readonly MarketContext _context;
    private readonly CommandDispatcher _commandDispatcher;
    private readonly QueryDispatcher _queryDispatcher;

    public MarketService(MarketContext context, CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
    {
        _context = context;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
    }

    // Get all active markets
    public async Task<List<Market>> GetActiveMarketsAsync()
    {
        return await _queryDispatcher.ExecuteAsync<GetActiveMarketsQuery, List<Market>>(new GetActiveMarketsQuery());
    }

    // Get market by ID
    public async Task<Market?> GetMarketByIdAsync(int marketId)
    {
        return await _queryDispatcher.ExecuteAsync<GetMarketByIdQuery, Market?>(new GetMarketByIdQuery(marketId));
    }

    // Calculate current market prices using Constant Product Market Maker (CPMM) formula
    public (decimal yesPrice, decimal noPrice) GetCurrentPrices(Market market)
    {
        // Using constant product AMM: x * y = k
        // Price of Yes = NoPool / (YesPool + NoPool)
        // Price of No = YesPool / (YesPool + NoPool)

        decimal total = market.YesPool + market.NoPool;

        if (total == 0)
        {
            return (0.5m, 0.5m); // Default to 50/50 if no liquidity
        }

        decimal yesPrice = market.NoPool / total;
        decimal noPrice = market.YesPool / total;

        // Ensure prices are between 0.01 and 0.99
        yesPrice = Math.Max(0.01m, Math.Min(0.99m, yesPrice));
        noPrice = Math.Max(0.01m, Math.Min(0.99m, noPrice));

        return (yesPrice, noPrice);
    }

    // Calculate cost for buying shares (includes slippage)
    public decimal CalculateBuyCost(Market market, Outcome outcome, decimal shares)
    {
        decimal k = market.YesPool * market.NoPool; // Constant product

        if (outcome == Outcome.Yes)
        {
            // Buying Yes: NoPool decreases, YesPool increases
            decimal newNoPool = market.NoPool / (1 + shares / market.YesPool);
            decimal cost = market.NoPool - newNoPool;
            return Math.Round(cost, 2);
        }
        else
        {
            // Buying No: YesPool decreases, NoPool increases
            decimal newYesPool = market.YesPool / (1 + shares / market.NoPool);
            decimal cost = market.YesPool - newYesPool;
            return Math.Round(cost, 2);
        }
    }

    // Calculate proceeds from selling shares
    public decimal CalculateSellProceeds(Market market, Outcome outcome, decimal shares)
    {
        if (outcome == Outcome.Yes)
        {
            // Selling Yes: NoPool increases, YesPool decreases
            decimal proceeds = market.NoPool * shares / (market.YesPool - shares);
            return Math.Round(proceeds, 2);
        }
        else
        {
            // Selling No: YesPool increases, NoPool decreases
            decimal proceeds = market.YesPool * shares / (market.NoPool - shares);
            return Math.Round(proceeds, 2);
        }
    }

    // Place a buy order
    public async Task<(bool success, string message)> PlaceBuyOrderAsync(int marketId, int userId, Outcome outcome, decimal shares)
    {
        var command = new PlaceBuyOrderCommand
        {
            MarketId = marketId,
            UserId = userId,
            Outcome = outcome,
            Shares = shares
        };

        var result = await _commandDispatcher.ExecuteAsync<PlaceBuyOrderCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Place a sell order
    public async Task<(bool success, string message)> PlaceSellOrderAsync(int marketId, int userId, Outcome outcome, decimal shares)
    {
        var command = new PlaceSellOrderCommand
        {
            MarketId = marketId,
            UserId = userId,
            Outcome = outcome,
            Shares = shares
        };

        var result = await _commandDispatcher.ExecuteAsync<PlaceSellOrderCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Get recent orders
    public async Task<List<Order>> GetRecentOrdersAsync(int marketId, int count = 20)
    {
        return await _queryDispatcher.ExecuteAsync<GetRecentOrdersQuery, List<Order>>(new GetRecentOrdersQuery(marketId, count));
    }

    // Resolve a market (admin function)
    public async Task<(bool success, string message)> ResolveMarketAsync(int marketId, bool outcome)
    {
        var command = new ResolveMarketCommand
        {
            MarketId = marketId,
            Outcome = outcome
        };

        var result = await _commandDispatcher.ExecuteAsync<ResolveMarketCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Create a new market
    public async Task<(bool success, string message, int marketId)> CreateMarketAsync(Market market)
    {
        var command = new CreateMarketCommand
        {
            Title = market.Title,
            Description = market.Description,
            Category = market.Category,
            EndDate = market.EndDate,
            InitialLiquidity = market.InitialLiquidity,
            MarketType = market.MarketType,
            OutcomeNames = market.MarketType == MarketType.MultiOutcome
                ? market.Outcomes?.Select(o => o.Name).ToList()
                : null
        };

        var result = await _commandDispatcher.ExecuteAsync<CreateMarketCommand, CommandResult<int>>(command);
        return (result.Success, result.Message, result.Data);
    }

    // ==================== MULTI-OUTCOME MARKET METHODS ====================

    // Get market with outcomes loaded
    public async Task<Market?> GetMarketWithOutcomesAsync(int marketId)
    {
        return await _queryDispatcher.ExecuteAsync<GetMarketWithOutcomesQuery, Market?>(new GetMarketWithOutcomesQuery(marketId));
    }

    // Get current prices for all outcomes in a multi-outcome market
    public Dictionary<int, decimal> GetMultiOutcomePrices(List<MarketOutcome> outcomes)
    {
        var prices = new Dictionary<int, decimal>();
        decimal totalLiquidity = outcomes.Sum(o => o.LiquidityPool);

        if (totalLiquidity == 0)
        {
            // Equal probability if no liquidity
            decimal equalPrice = 1.0m / outcomes.Count;
            foreach (var outcome in outcomes)
            {
                prices[outcome.Id] = Math.Round(equalPrice, 4);
            }
            return prices;
        }

        // Price = Other outcomes' liquidity / Total liquidity
        // This ensures prices sum to approximately 1
        foreach (var outcome in outcomes)
        {
            decimal otherLiquidity = totalLiquidity - outcome.LiquidityPool;
            decimal price = otherLiquidity / totalLiquidity;

            // Clamp price between 0.01 and 0.99
            price = Math.Max(0.01m, Math.Min(0.99m, price));
            prices[outcome.Id] = Math.Round(price, 4);
        }

        return prices;
    }

    // Calculate cost for buying shares in a multi-outcome market
    public decimal CalculateMultiOutcomeBuyCost(List<MarketOutcome> outcomes, int outcomeId, decimal shares)
    {
        var outcome = outcomes.FirstOrDefault(o => o.Id == outcomeId);
        if (outcome == null) return 0;

        // Simple linear pricing for now
        // Future enhancement: implement proper AMM pricing
        var prices = GetMultiOutcomePrices(outcomes);
        decimal price = prices.GetValueOrDefault(outcomeId, 0.5m);

        return Math.Round(shares * price, 2);
    }

    // Place buy order for multi-outcome market
    public async Task<(bool success, string message)> PlaceMultiOutcomeBuyOrderAsync(
        int marketId, int userId, int outcomeId, decimal shares)
    {
        var command = new PlaceMultiOutcomeBuyOrderCommand
        {
            MarketId = marketId,
            UserId = userId,
            MarketOutcomeId = outcomeId,
            Shares = shares
        };

        var result = await _commandDispatcher.ExecuteAsync<PlaceMultiOutcomeBuyOrderCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }

    // Place sell order for multi-outcome market
    public async Task<(bool success, string message)> PlaceMultiOutcomeSellOrderAsync(
        int marketId, int userId, int outcomeId, decimal shares)
    {
        var command = new PlaceMultiOutcomeSellOrderCommand
        {
            MarketId = marketId,
            UserId = userId,
            MarketOutcomeId = outcomeId,
            Shares = shares
        };

        var result = await _commandDispatcher.ExecuteAsync<PlaceMultiOutcomeSellOrderCommand, CommandResult>(command);
        return (result.Success, result.Message);
    }
}
