using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Command.Common;
using PolyMyMarket.Context;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Handler for GetOrCreateUserCommand
/// </summary>
public class GetOrCreateUserCommandHandler : ICommandHandler<GetOrCreateUserCommand, CommandResult<int>>
{
    private readonly MarketContext _context;

    public GetOrCreateUserCommandHandler(MarketContext context)
    {
        _context = context;
    }

    public async Task<CommandResult<int>> HandleAsync(GetOrCreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Validate email
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return CommandResult<int>.FailureResult("Email is required");
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return CommandResult<int>.FailureResult("Name is required");
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (existingUser != null)
        {
            return CommandResult<int>.SuccessResult("User retrieved successfully", existingUser.Id);
        }

        // Create new user
        var user = new Models.User
        {
            Email = command.Email,
            Name = command.Name,
            Balance = command.InitialBalance,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return CommandResult<int>.SuccessResult("User created successfully", user.Id);
    }
}
