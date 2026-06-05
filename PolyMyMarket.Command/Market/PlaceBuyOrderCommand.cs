using PolyMyMarket.Command.Common;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Command to place a buy order in a binary prediction market
/// </summary>
public class PlaceBuyOrderCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public int UserId { get; set; }
    public Outcome Outcome { get; set; }
    public decimal Shares { get; set; }
}
