using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Web.Services;

public class MarketService
{
    private readonly MarketContext _context;

    public MarketService(MarketContext context)
    {
        _context = context;
    }

    // Get all active markets
    public async Task<List<Market>> GetActiveMarketsAsync()
    {
        return await _context.Markets
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
        var market = await _context.Markets.FindAsync(marketId);
        if (market == null)
            return (false, "Market not found");

        if (market.Status != MarketStatus.Active)
            return (false, "Market is not active");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");

        // Calculate cost
        decimal cost = CalculateBuyCost(market, outcome, shares);

        if (user.Balance < cost)
            return (false, $"Insufficient balance. Cost: ${cost:F2}, Balance: ${user.Balance:F2}");

        var (yesPrice, noPrice) = GetCurrentPrices(market);
        decimal price = outcome == Outcome.Yes ? yesPrice : noPrice;

        // Create order
        var order = new Order
        {
            MarketId = marketId,
            UserId = userId,
            Outcome = outcome,
            Shares = shares,
            Price = price,
            OrderType = OrderType.Buy,
            TotalCost = cost,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        user.Balance -= cost;

        // Update market pools
        if (outcome == Outcome.Yes)
        {
            market.YesPool += shares;
            market.NoPool -= cost;
        }
        else
        {
            market.NoPool += shares;
            market.YesPool -= cost;
        }

        // Update or create position
        await UpdatePositionAsync(userId, marketId, outcome, shares, cost, true);

        await _context.SaveChangesAsync();

        return (true, $"Successfully bought {shares} {outcome} shares for ${cost:F2}");
    }

    // Place a sell order
    public async Task<(bool success, string message)> PlaceSellOrderAsync(int marketId, int userId, Outcome outcome, decimal shares)
    {
        var market = await _context.Markets.FindAsync(marketId);
        if (market == null)
            return (false, "Market not found");

        if (market.Status != MarketStatus.Active)
            return (false, "Market is not active");

        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketId == marketId);

        if (position == null)
            return (false, "You don't have a position in this market");

        decimal availableShares = outcome == Outcome.Yes ? position.YesShares : position.NoShares;

        if (availableShares < shares)
            return (false, $"Insufficient shares. You have {availableShares} {outcome} shares");

        // Calculate proceeds
        decimal proceeds = CalculateSellProceeds(market, outcome, shares);

        var (yesPrice, noPrice) = GetCurrentPrices(market);
        decimal price = outcome == Outcome.Yes ? yesPrice : noPrice;

        // Create order
        var order = new Order
        {
            MarketId = marketId,
            UserId = userId,
            Outcome = outcome,
            Shares = shares,
            Price = price,
            OrderType = OrderType.Sell,
            TotalCost = proceeds,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.Balance += proceeds;
        }

        // Update market pools
        if (outcome == Outcome.Yes)
        {
            market.YesPool -= shares;
            market.NoPool += proceeds;
        }
        else
        {
            market.NoPool -= shares;
            market.YesPool += proceeds;
        }

        // Update position
        await UpdatePositionAsync(userId, marketId, outcome, shares, proceeds, false);

        await _context.SaveChangesAsync();

        return (true, $"Successfully sold {shares} {outcome} shares for ${proceeds:F2}");
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
        var market = await _context.Markets
            .Include(m => m.Positions)
            .FirstOrDefaultAsync(m => m.Id == marketId);

        if (market == null)
            return (false, "Market not found");

        if (market.Status == MarketStatus.Resolved)
            return (false, "Market already resolved");

        market.Status = MarketStatus.Resolved;
        market.ResolvedOutcome = outcome;
        market.ResolutionDate = DateTime.UtcNow;

        // Pay out winning positions
        foreach (var position in market.Positions)
        {
            var user = await _context.Users.FindAsync(position.UserId);
            if (user != null)
            {
                decimal payout = outcome ? position.YesShares : position.NoShares;
                user.Balance += payout;
            }
        }

        await _context.SaveChangesAsync();

        return (true, $"Market resolved to {(outcome ? "Yes" : "No")}. Payouts distributed.");
    }

    // Create a new market
    public async Task<(bool success, string message, int marketId)> CreateMarketAsync(Market market)
    {
        try
        {
            // Validate market data
            if (string.IsNullOrWhiteSpace(market.Title))
                return (false, "Market title is required", 0);

            if (string.IsNullOrWhiteSpace(market.Description))
                return (false, "Market description is required", 0);

            if (market.EndDate <= DateTime.UtcNow)
                return (false, "End date must be in the future", 0);

            if (market.InitialLiquidity < 100)
                return (false, "Initial liquidity must be at least $100", 0);

            // Ensure pools are balanced
            market.YesPool = market.InitialLiquidity / 2;
            market.NoPool = market.InitialLiquidity / 2;
            market.Status = MarketStatus.Active;
            market.CreatedDate = DateTime.UtcNow;

            _context.Markets.Add(market);
            await _context.SaveChangesAsync();

            return (true, "Market created successfully", market.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Error creating market: {ex.Message}", 0);
        }
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
        var market = await GetMarketWithOutcomesAsync(marketId);
        if (market == null)
            return (false, "Market not found");

        if (market.Status != MarketStatus.Active)
            return (false, "Market is not active");

        if (market.MarketType != MarketType.MultiOutcome)
            return (false, "This market is not a multi-outcome market");

        var outcome = market.Outcomes.FirstOrDefault(o => o.Id == outcomeId);
        if (outcome == null)
            return (false, "Outcome not found");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");

        // Calculate cost
        decimal cost = CalculateMultiOutcomeBuyCost(market.Outcomes.ToList(), outcomeId, shares);

        if (user.Balance < cost)
            return (false, $"Insufficient balance. Cost: ${cost:F2}, Balance: ${user.Balance:F2}");

        var prices = GetMultiOutcomePrices(market.Outcomes.ToList());
        decimal price = prices.GetValueOrDefault(outcomeId, 0.5m);

        // Create order
        var order = new Order
        {
            MarketId = marketId,
            UserId = userId,
            MarketOutcomeId = outcomeId,
            Outcome = null, // Multi-outcome markets don't use the legacy enum
            Shares = shares,
            Price = price,
            OrderType = OrderType.Buy,
            TotalCost = cost,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        user.Balance -= cost;

        // Update outcome liquidity pool (add cost to this outcome's pool)
        outcome.LiquidityPool += cost;

        // Update or create outcome position
        await UpdateOutcomePositionAsync(userId, outcomeId, shares, cost, true);

        await _context.SaveChangesAsync();

        return (true, $"Successfully bought {shares} shares of '{outcome.Name}' for ${cost:F2}");
    }

    // Place sell order for multi-outcome market
    public async Task<(bool success, string message)> PlaceMultiOutcomeSellOrderAsync(
        int marketId, int userId, int outcomeId, decimal shares)
    {
        var market = await GetMarketWithOutcomesAsync(marketId);
        if (market == null)
            return (false, "Market not found");

        if (market.Status != MarketStatus.Active)
            return (false, "Market is not active");

        if (market.MarketType != MarketType.MultiOutcome)
            return (false, "This market is not a multi-outcome market");

        var outcome = market.Outcomes.FirstOrDefault(o => o.Id == outcomeId);
        if (outcome == null)
            return (false, "Outcome not found");

        // Check position
        var position = await _context.OutcomePositions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketOutcomeId == outcomeId);

        if (position == null || position.Shares < shares)
            return (false, $"Insufficient shares. You have {position?.Shares ?? 0} shares");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");

        // Calculate proceeds (simple: current price * shares)
        var prices = GetMultiOutcomePrices(market.Outcomes.ToList());
        decimal price = prices.GetValueOrDefault(outcomeId, 0.5m);
        decimal proceeds = Math.Round(shares * price, 2);

        // Ensure outcome has enough liquidity to pay out
        if (outcome.LiquidityPool < proceeds)
            proceeds = outcome.LiquidityPool;

        // Create order
        var order = new Order
        {
            MarketId = marketId,
            UserId = userId,
            MarketOutcomeId = outcomeId,
            Outcome = null,
            Shares = shares,
            Price = price,
            OrderType = OrderType.Sell,
            TotalCost = proceeds,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        user.Balance += proceeds;

        // Update outcome liquidity pool
        outcome.LiquidityPool -= proceeds;

        // Update outcome position
        await UpdateOutcomePositionAsync(userId, outcomeId, shares, proceeds, false);

        await _context.SaveChangesAsync();

        return (true, $"Successfully sold {shares} shares of '{outcome.Name}' for ${proceeds:F2}");
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
