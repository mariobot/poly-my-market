using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Command to place a sell order in a binary prediction market
/// </summary>
public class PlaceSellOrderCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public int UserId { get; set; }
    public Outcome Outcome { get; set; }
    public decimal Shares { get; set; }
}

/// <summary>
/// Handler for placing sell orders in binary prediction markets
/// </summary>
public class PlaceSellOrderCommandHandler : ICommandHandler<PlaceSellOrderCommand, CommandResult>
{
    private readonly MarketContext _context;

    public PlaceSellOrderCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(PlaceSellOrderCommand command, CancellationToken cancellationToken = default)
    {
        var market = await _context.Markets.FindAsync(new object[] { command.MarketId }, cancellationToken);
        if (market == null)
            return CommandResult.FailureResult("Market not found");

        if (market.Status != MarketStatus.Active)
            return CommandResult.FailureResult("Market is not active");

        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == command.UserId && p.MarketId == command.MarketId, cancellationToken);

        if (position == null)
            return CommandResult.FailureResult("You don't have a position in this market");

        decimal availableShares = command.Outcome == Outcome.Yes ? position.YesShares : position.NoShares;

        if (availableShares < command.Shares)
            return CommandResult.FailureResult($"Insufficient shares. You have {availableShares} {command.Outcome} shares");

        // Calculate proceeds
        decimal proceeds = CalculateSellProceeds(market, command.Outcome, command.Shares);

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
            OrderType = OrderType.Sell,
            TotalCost = proceeds,
            Timestamp = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Update user balance
        var user = await _context.Users.FindAsync(new object[] { command.UserId }, cancellationToken);
        if (user != null)
        {
            user.Balance += proceeds;
        }

        // Update market pools
        if (command.Outcome == Outcome.Yes)
        {
            market.YesPool -= command.Shares;
            market.NoPool += proceeds;
        }
        else
        {
            market.NoPool -= command.Shares;
            market.YesPool += proceeds;
        }

        // Update position
        await UpdatePositionAsync(command.UserId, command.MarketId, command.Outcome, command.Shares, proceeds, false, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult($"Successfully sold {command.Shares} {command.Outcome} shares for ${proceeds:F2}");
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
    /// Calculate proceeds from selling shares
    /// </summary>
    private decimal CalculateSellProceeds(Models.Market market, Outcome outcome, decimal shares)
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

    /// <summary>
    /// Update user position after sell order
    /// </summary>
    private async Task UpdatePositionAsync(int userId, int marketId, Outcome outcome, decimal shares, decimal amount, bool isBuy, CancellationToken cancellationToken)
    {
        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MarketId == marketId, cancellationToken);

        if (position == null)
            return; // Should not happen as we validated earlier

        // Sell operation
        if (!isBuy)
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

            position.LastUpdated = DateTime.UtcNow;
        }
    }

    #endregion
}
