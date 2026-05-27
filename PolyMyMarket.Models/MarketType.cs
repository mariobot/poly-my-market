namespace PolyMyMarket.Models;

/// <summary>
/// Defines the type of prediction market
/// </summary>
public enum MarketType
{
    /// <summary>
    /// Traditional Yes/No binary outcome market
    /// </summary>
    Binary = 0,

    /// <summary>
    /// Multiple outcome market (e.g., election with 4 candidates)
    /// </summary>
    MultiOutcome = 1
}
