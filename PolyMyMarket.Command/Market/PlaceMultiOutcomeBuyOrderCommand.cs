using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

public class PlaceMultiOutcomeBuyOrderCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public int UserId { get; set; }
    public int MarketOutcomeId { get; set; }
    public decimal Shares { get; set; }
}

public class PlaceMultiOutcomeBuyOrderCommandHandler : ICommandHandler<PlaceMultiOutcomeBuyOrderCommand, CommandResult>
{
    private readonly MarketContext _context;

    public PlaceMultiOutcomeBuyOrderCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(PlaceMultiOutcomeBuyOrderCommand command, CancellationToken cancellationToken = default)
    {
        // Load market with outcomes
        var market = await _context.Markets
            .Include(m => m.Outcomes)
            .FirstOrDefaultAsync(m => m.Id == command.MarketId, cancellationToken);

        if (market == null)
        {
            return CommandResult.FailureResult("Market not found");
        }

        if (market.Status != MarketStatus.Active)
        {
            return CommandResult.FailureResult("Market is not active");
        }

        if (market.MarketType != MarketType.MultiOutcome)
        {
            return CommandResult.FailureResult("This market is not a multi-outcome market");
        }

        var outcome = market.Outcomes.FirstOrDefault(o => o.Id == command.MarketOutcomeId);
        if (outcome == null)
        {
            return CommandResult.FailureResult("Outcome not found");
        }

        var user = await _context.Users.FindAsync(new object[] { command.UserId }, cancellationToken);
        if (user == null)
        {
            return CommandResult.FailureResult("User not found");
        }

        // Calculate cost using multi-outcome pricing
        decimal cost = CalculateMultiOutcomeBuyCost(market.Outcomes.ToList(), command.MarketOutcomeId, command.Shares);

        if (user.Balance < cost)
        {
            return CommandResult.FailureResult($"Insufficient balance. Cost: ${cost:F2}, Balance: ${user.Balance:F2}");
        }

        var prices = GetMultiOutcomePrices(market.Outcomes.ToList());
        decimal price = prices.GetValueOrDefault(command.MarketOutcomeId, 0.5m);

        // Create order
        var order = new Order
        {
            MarketId = command.MarketId,
            UserId = command.UserId,
            MarketOutcomeId = command.MarketOutcomeId,
            Outcome = null, // Multi-outcome markets don't use the legacy enum
            Shares = command.Shares,
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
        await UpdateOutcomePositionAsync(command.UserId, command.MarketOutcomeId, command.Shares, cost, true, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult($"Successfully bought {command.Shares} shares of '{outcome.Name}' for ${cost:F2}");
    }

    // Get current prices for all outcomes in a multi-outcome market
    private Dictionary<int, decimal> GetMultiOutcomePrices(List<MarketOutcome> outcomes)
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
    private decimal CalculateMultiOutcomeBuyCost(List<MarketOutcome> outcomes, int outcomeId, decimal shares)
    {
        var outcome = outcomes.FirstOrDefault(o => o.Id == outcomeId);
        if (outcome == null) return 0;

        // Simple linear pricing for now
        var prices = GetMultiOutcomePrices(outcomes);
        decimal price = prices.GetValueOrDefault(outcomeId, 0.5m);

        return Math.Round(shares * price, 2);
    }

    // Update user outcome position
    private async Task UpdateOutcomePositionAsync(int userId, int outcomeId, decimal shares, decimal amount, bool isBuy, CancellationToken cancellationToken)
    {
        var position = await _context.OutcomePositions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketOutcomeId == outcomeId, cancellationToken);

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
