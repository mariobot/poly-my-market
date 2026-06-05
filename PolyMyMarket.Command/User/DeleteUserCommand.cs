using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to delete a user
/// </summary>
public class DeleteUserCommand : ICommand<CommandResult>
{
    public int UserId { get; set; }
}
