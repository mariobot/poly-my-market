using PolyMyMarket.Command.Common;

namespace PolyMyMarket.Command.Market;

public class ResolveMarketCommand : ICommand<CommandResult>
{
    public int MarketId { get; set; }
    public bool Outcome { get; set; }  // true = Yes, false = No
}
