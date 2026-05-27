using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyMyMarket.Models;

/// <summary>
/// Represents a user's position (holdings) in a specific market outcome
/// Replaces the binary Position entity with a more flexible per-outcome model
/// </summary>
public class OutcomePosition
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int MarketOutcomeId { get; set; }

    /// <summary>
    /// Number of shares owned in this outcome
    /// </summary>
    public decimal Shares { get; set; } = 0m;

    /// <summary>
    /// Average price paid per share (for P&L calculation)
    /// </summary>
    public decimal AveragePrice { get; set; } = 0m;

    /// <summary>
    /// Total amount invested in this outcome
    /// </summary>
    public decimal TotalInvested { get; set; } = 0m;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public MarketOutcome MarketOutcome { get; set; } = null!;

    // Calculated methods (not stored in database)

    /// <summary>
    /// Calculate the current value of this position given current price
    /// </summary>
    public decimal CalculateCurrentValue(decimal currentPrice)
    {
        return Shares * currentPrice;
    }

    /// <summary>
    /// Calculate profit/loss for this position
    /// </summary>
    public decimal CalculateProfitLoss(decimal currentPrice)
    {
        return CalculateCurrentValue(currentPrice) - TotalInvested;
    }

    /// <summary>
    /// Calculate profit/loss percentage
    /// </summary>
    public decimal CalculateProfitLossPercent(decimal currentPrice)
    {
        if (TotalInvested == 0) return 0;
        return (CalculateProfitLoss(currentPrice) / TotalInvested) * 100;
    }
}
