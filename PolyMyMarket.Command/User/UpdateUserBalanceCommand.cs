using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.User;

/// <summary>
/// Command to update user balance
/// </summary>
public class UpdateUserBalanceCommand : ICommand<CommandResult>
{
    public int UserId { get; set; }
    public decimal NewBalance { get; set; }
}
