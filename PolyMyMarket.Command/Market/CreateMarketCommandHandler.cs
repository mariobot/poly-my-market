using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Handler for creating new prediction markets
/// </summary>
public class CreateMarketCommandHandler : ICommandHandler<CreateMarketCommand, CommandResult<int>>
{
    private readonly MarketContext _context;

    public CreateMarketCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult<int>> HandleAsync(CreateMarketCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate market data
            if (string.IsNullOrWhiteSpace(command.Title))
                return CommandResult<int>.FailureResult("Market title is required");

            if (string.IsNullOrWhiteSpace(command.Description))
                return CommandResult<int>.FailureResult("Market description is required");

            if (command.EndDate <= DateTime.UtcNow)
                return CommandResult<int>.FailureResult("End date must be in the future");

            if (command.InitialLiquidity < 100)
                return CommandResult<int>.FailureResult("Initial liquidity must be at least $100");

            // Create the market
            var market = new Models.Market
            {
                Title = command.Title,
                Description = command.Description,
                Category = command.Category,
                EndDate = command.EndDate,
                InitialLiquidity = command.InitialLiquidity,
                MarketType = command.MarketType,
                Status = MarketStatus.Active,
                CreatedDate = DateTime.UtcNow
            };

            // Set up pools based on market type
            if (command.MarketType == MarketType.Binary)
            {
                // Binary market: split liquidity evenly between Yes and No
                market.YesPool = command.InitialLiquidity / 2;
                market.NoPool = command.InitialLiquidity / 2;
            }
            else if (command.MarketType == MarketType.MultiOutcome)
            {
                // Multi-outcome market: create outcomes
                if (command.OutcomeNames == null || command.OutcomeNames.Count < 2)
                    return CommandResult<int>.FailureResult("Multi-outcome markets must have at least 2 outcomes");

                if (command.OutcomeNames.Count > 10)
                    return CommandResult<int>.FailureResult("Multi-outcome markets cannot have more than 10 outcomes");

                // Binary pools not used for multi-outcome
                market.YesPool = 0;
                market.NoPool = 0;

                // Create market outcomes with equal initial liquidity
                decimal liquidityPerOutcome = command.InitialLiquidity / command.OutcomeNames.Count;

                for (int i = 0; i < command.OutcomeNames.Count; i++)
                {
                    var outcome = new MarketOutcome
                    {
                        Name = command.OutcomeNames[i],
                        DisplayOrder = i,
                        LiquidityPool = liquidityPerOutcome
                    };
                    market.Outcomes.Add(outcome);
                }
            }

            _context.Markets.Add(market);
            await _context.SaveChangesAsync(cancellationToken);

            return CommandResult<int>.SuccessResult("Market created successfully", market.Id);
        }
        catch (Exception ex)
        {
            return CommandResult<int>.FailureResult($"Error creating market: {ex.Message}");
        }
    }
}
