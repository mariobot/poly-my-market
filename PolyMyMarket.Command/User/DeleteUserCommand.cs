using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to delete a user
/// </summary>
public class DeleteUserCommand : ICommand<CommandResult>
{
    public int UserId { get; set; }
}

/// <summary>
/// Handler for DeleteUserCommand
/// </summary>
public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, CommandResult>
{
    private readonly MarketContext _context;

    public DeleteUserCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        // Find user with related data
        var user = await _context.Users
            .Include(u => u.Orders)
            .Include(u => u.Positions)
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user == null)
        {
            return CommandResult.FailureResult("User not found");
        }

        // Check if user has active positions (binary markets)
        if (user.Positions.Any(p => p.YesShares > 0 || p.NoShares > 0))
        {
            return CommandResult.FailureResult("Cannot delete user with active positions");
        }

        // Remove user (cascade will handle orders and empty positions)
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult("User deleted successfully");
    }
}
