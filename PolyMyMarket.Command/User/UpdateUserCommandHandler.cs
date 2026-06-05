using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Handler for UpdateUserCommand
/// </summary>
public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, CommandResult>
{
    private readonly MarketContext _context;

    public UpdateUserCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return CommandResult.FailureResult("Name is required");
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return CommandResult.FailureResult("Email is required");
        }

        // Find user
        var user = await _context.Users.FindAsync(new object[] { command.UserId }, cancellationToken);
        if (user == null)
        {
            return CommandResult.FailureResult("User not found");
        }

        // Check if email is being changed to one that already exists
        if (user.Email != command.Email)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email && u.Id != command.UserId, cancellationToken);

            if (existingUser != null)
            {
                return CommandResult.FailureResult("Email already exists");
            }
        }

        // Validate balance
        if (command.Balance < 0)
        {
            return CommandResult.FailureResult("Balance cannot be negative");
        }

        // Update user
        user.Name = command.Name;
        user.Email = command.Email;
        user.Balance = command.Balance;

        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult.SuccessResult("User updated successfully");
    }
}
