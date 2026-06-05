using PolyMyMarket.Command.Common;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Command to place a sell order in a binary prediction market
/// </summary>
public class PlaceSellOrderCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public int UserId { get; set; }
    public Outcome Outcome { get; set; }
    public decimal Shares { get; set; }
}
