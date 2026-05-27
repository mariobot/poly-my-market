using System.ComponentModel.DataAnnotations;

namespace PolyMyMarket.Models;

/// <summary>
/// Represents a single outcome option in a prediction market
/// Examples: "Yes", "No" for binary; "Candidate A", "Candidate B", etc. for multi-outcome
/// </summary>
public class MarketOutcome
{
    public int Id { get; set; }

    [Required]
    public int MarketId { get; set; }

    /// <summary>
    /// Name of the outcome (e.g., "Yes", "Candidate A", "Over 50 goals")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed description of this outcome
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display order in UI (1, 2, 3, etc.)
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Liquidity pool for this outcome (AMM formula)
    /// </summary>
    public decimal LiquidityPool { get; set; } = 0m;

    /// <summary>
    /// Whether this outcome won when market resolved
    /// </summary>
    public bool IsWinner { get; set; } = false;

    // Navigation properties
    public Market Market { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<OutcomePosition> Positions { get; set; } = new List<OutcomePosition>();
}
