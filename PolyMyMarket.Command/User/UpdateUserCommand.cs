using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to update user details
/// </summary>
public class UpdateUserCommand : ICommand<CommandResult>
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
