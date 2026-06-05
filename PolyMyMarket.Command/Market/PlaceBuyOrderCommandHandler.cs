using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Handler for placing buy orders in binary prediction markets
/// </summary>
public class PlaceBuyOrderCommandHandler : ICommandHandler<PlaceBuyOrderCommand, CommandResult>
{
    private readonly MarketContext _context;

    public PlaceBuyOrderCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(PlaceBuyOrderCommand command, CancellationToken cancellationToken = default)
    {
        var market = await _context.Markets.FindAsync(new object[] { command.MarketId }, cancellationToken);
        if (market == null)
            return CommandResult.FailureResult("Market not found");

        if (market.Status != MarketStatus.Active)
            return CommandResult.FailureResult("Market is not active");

        var user = await _context.Users.FindAsync(new object[] { command.UserId }, cancellationToken);
        if (user == null)
            return CommandResult.FailureResult("User not found");

        // Calculate cost using AMM formula
        decimal cost = CalculateBuyCost(market, command.Outcome, command.Shares);

        if (user.Balance < cost)
            return CommandResult.FailureResult($"Insufficient balance. Cost: ${cost:F2}, Balance: ${user.Balance:F2}");

        var (yesPrice, noPrice) = GetCurrentPrices(market);
        decimal price = command.Outcome == Outcome.Yes ? yesPrice : noPrice;

        // Create order
        var order = new Order
        {
            MarketId = command.MarketId,
            UserId = command.UserId,
            Outcome = command.Outcome,
            Shares = command.Shares,
            Price = price,
            OrderType = OrderType.Buy,
            TotalCost = cost,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        user.Balance -= cost;

        // Update market pools
        if (command.Outcome == Outcome.Yes)
        {
            market.YesPool += command.Shares;
            market.NoPool -= cost;
        }
        else
        {
            market.NoPool += command.Shares;
            market.YesPool -= cost;
        }

        // Update or create position
        await UpdatePositionAsync(command.UserId, command.MarketId, command.Outcome, command.Shares, cost, true, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult($"Successfully bought {command.Shares} {command.Outcome} shares for ${cost:F2}");
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculate current market prices using Constant Product Market Maker (CPMM) formula
    /// </summary>
    private (decimal yesPrice, decimal noPrice) GetCurrentPrices(Models.Market market)
    {
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

    /// <summary>
    /// Calculate cost for buying shares (includes slippage)
    /// </summary>
    private decimal CalculateBuyCost(Models.Market market, Outcome outcome, decimal shares)
    {
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

    /// <summary>
    /// Update user position after buy order
    /// </summary>
    private async Task UpdatePositionAsync(int userId, int marketId, Outcome outcome, decimal shares, decimal amount, bool isBuy, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketId == marketId, cancellationToken);

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
                    position.YesShares += shares;
                    position.TotalInvestedYes += amount;
                    position.AveragePriceYes = position.TotalInvestedYes / position.YesShares;
                }
                else
                {
                    position.NoShares += shares;
                    position.TotalInvestedNo += amount;
                    position.AveragePriceNo = position.TotalInvestedNo / position.NoShares;
                }
            }

            position.LastUpdated = DateTime.UtcNow;
        }
    }

    #endregion
}
