using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

public class ResolveMarketCommandHandler : ICommandHandler<ResolveMarketCommand, CommandResult>
{
    private readonly MarketContext _context;

    public ResolveMarketCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(ResolveMarketCommand command, CancellationToken cancellationToken = default)
    {
        // Load market with positions
        var market = await _context.Markets
            .Include(m => m.Positions)
            .FirstOrDefaultAsync(m => m.Id == command.MarketId, cancellationToken);

        if (market == null)
        {
            return CommandResult.FailureResult("Market not found");
        }

        if (market.Status == MarketStatus.Resolved)
        {
            return CommandResult.FailureResult("Market already resolved");
        }

        // Update market status
        market.Status = MarketStatus.Resolved;
        market.ResolvedOutcome = command.Outcome;
        market.ResolutionDate = DateTime.UtcNow;

        // Pay out winning positions
        foreach (var position in market.Positions)
        {
            var user = await _context.Users.FindAsync(new object[] { position.UserId }, cancellationToken);
            if (user != null)
            {
                decimal payout = command.Outcome ? position.YesShares : position.NoShares;
                user.Balance += payout;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult(
            $"Market resolved to {(command.Outcome ? "Yes" : "No")}. Payouts distributed."
        );
    }
}
