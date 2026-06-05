using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Command.Market;

namespace PolyMyMarket.Web.Services;

public class MarketService
{
    private readonly MarketContext _context;
    private readonly CommandDispatcher _commandDispatcher;

    public MarketService(MarketContext context, CommandDispatcher commandDispatcher)
    {
        _context = context;
        _commandDispatcher = commandDispatcher;
    }

    // Get all active markets
    public async Task<List<Market>> GetActiveMarketsAsync()
    {
        return await _context.Markets
            .Include(m => m.Outcomes.OrderBy(o => o.DisplayOrder))
            .Where(m => m.Status == MarketStatus.Active)
            .OrderByDescending(m => m.CreatedDate)
            .ToListAsync();
    }

    // Get market by ID
    public async Task<Market?> GetMarketByIdAsync(int marketId)
    {
        return await _context.Markets
            .Include(m => m.Orders.OrderByDescending(o => o.Timestamp).Take(20))
            .FirstOrDefaultAsync(m => m.Id == marketId);
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

    // Update user position
    private async Task UpdatePositionAsync(int userId, int marketId, Outcome outcome, decimal shares, decimal amount, bool isBuy)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketId == marketId);

        if (position == null)
        {
            // Create new position
            position = new Position
            {
                UserId = userId,
                MarketId = marketId,
                YesShares = outcome == Outcome.Yes && isBuy ? shares : 0,
                NoShares = outcome == Outcome.No && isBuy ? shares : 0,
                AveragePriceYes = outcome == Outcome.Yes && isBuy ? amount / shares : 0,
                AveragePriceNo = outcome == Outcome.No && isBuy ? amount / shares : 0,
                TotalInvestedYes = outcome == Outcome.Yes && isBuy ? amount : 0,
                TotalInvestedNo = outcome == Outcome.No && isBuy ? amount : 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.Positions.Add(position);
        }
        else
        {
            // Update existing position
            if (isBuy)
            {
                if (outcome == Outcome.Yes)
                {
                    decimal oldTotal = position.YesShares * position.AveragePriceYes;
                    position.YesShares += shares;
                    position.TotalInvestedYes += amount;
                    position.AveragePriceYes = position.TotalInvestedYes / position.YesShares;
                }
                else
                {
                    decimal oldTotal = position.NoShares * position.AveragePriceNo;
                    position.NoShares += shares;
                    position.TotalInvestedNo += amount;
                    position.AveragePriceNo = position.TotalInvestedNo / position.NoShares;
                }
            }
            else // sell
            {
                if (outcome == Outcome.Yes)
                {
                    decimal percentSold = shares / position.YesShares;
                    position.YesShares -= shares;
                    position.TotalInvestedYes -= position.TotalInvestedYes * percentSold;
                    // Average price stays the same
                }
                else
                {
                    decimal percentSold = shares / position.NoShares;
                    position.NoShares -= shares;
                    position.TotalInvestedNo -= position.TotalInvestedNo * percentSold;
                    // Average price stays the same
                }
            }

            position.LastUpdated = DateTime.UtcNow;
        }
    }

    // Get recent orders for a market
    public async Task<List<Order>> GetRecentOrdersAsync(int marketId, int count = 20)
    {
        return await _context.Orders
            .Where(o => o.MarketId == marketId)
            .OrderByDescending(o => o.Timestamp)
            .Take(count)
            .Include(o => o.User)
            .ToListAsync();
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
        return await _context.Markets
            .Include(m => m.Outcomes.OrderBy(o => o.DisplayOrder))
            .Include(m => m.Orders.OrderByDescending(o => o.Timestamp).Take(20))
                .ThenInclude(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == marketId);
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

    // Update user outcome position
    private async Task UpdateOutcomePositionAsync(int userId, int outcomeId, decimal shares, decimal amount, bool isBuy)
    {
        var position = await _context.OutcomePositions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketOutcomeId == outcomeId);

        if (position == null)
        {
            if (!isBuy) return; // Can't sell if no position exists

            // Create new position
            position = new OutcomePosition
            {
                UserId = userId,
                MarketOutcomeId = outcomeId,
                Shares = shares,
                AveragePrice = amount / shares,
                TotalInvested = amount,
                LastUpdated = DateTime.UtcNow
            };
            _context.OutcomePositions.Add(position);
        }
        else
        {
            if (isBuy)
            {
                // Add to position
                position.TotalInvested += amount;
                position.Shares += shares;
                position.AveragePrice = position.TotalInvested / position.Shares;
            }
            else
            {
                // Reduce position
                decimal percentSold = shares / position.Shares;
                position.Shares -= shares;
                position.TotalInvested -= position.TotalInvested * percentSold;
                // Average price stays the same
            }

            position.LastUpdated = DateTime.UtcNow;
        }
    }
}
