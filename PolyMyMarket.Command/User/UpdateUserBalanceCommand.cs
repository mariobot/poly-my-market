using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to update user balance
/// </summary>
public class UpdateUserBalanceCommand : ICommand<CommandResult>
{
    public int UserId { get; set; }
    public decimal NewBalance { get; set; }
}

/// <summary>
/// Handler for UpdateUserBalanceCommand
/// </summary>
public class UpdateUserBalanceCommandHandler : ICommandHandler<UpdateUserBalanceCommand, CommandResult>
{
    private readonly MarketContext _context;

    public UpdateUserBalanceCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(UpdateUserBalanceCommand command, CancellationToken cancellationToken = default)
    {
        // Validate balance
        if (command.NewBalance < 0)
        {
            return CommandResult.FailureResult("Balance cannot be negative");
        }

        // Find user
        var user = await _context.Users.FindAsync(new object[] { command.UserId }, cancellationToken);
        if (user == null)
        {
            return CommandResult.FailureResult("User not found");
        }

        // Update balance
        user.Balance = command.NewBalance;
        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult("User balance updated successfully");
    }
}
