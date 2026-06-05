using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to create or retrieve a user by email
/// </summary>
public class GetOrCreateUserCommand : ICommand<CommandResult<int>>
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; } = 10000m;
}
