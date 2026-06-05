using PolyMyMarket.Command.Common;
using PolyMyMarket.Models;

namespace PolyMyMarket.Command.Market;

/// <summary>
/// Command to create a new prediction market
/// </summary>
public class CreateMarketCommand : ICommand<CommandResult<int>>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public decimal InitialLiquidity { get; set; }
    public MarketType MarketType { get; set; }
    public List<string>? OutcomeNames { get; set; } // For multi-outcome markets
}
