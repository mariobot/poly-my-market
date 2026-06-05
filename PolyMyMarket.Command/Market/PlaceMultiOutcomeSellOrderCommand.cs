using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.Market;

public class PlaceMultiOutcomeSellOrderCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public int UserId { get; set; }
    public int MarketOutcomeId { get; set; }
    public decimal Shares { get; set; }
}
