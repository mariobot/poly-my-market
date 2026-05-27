using System.ComponentModel.DataAnnotations;

namespace PolyMyMarket.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public int MarketId { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// For multi-outcome markets: reference to specific MarketOutcome
    /// For binary markets: can be null (uses legacy Outcome enum instead)
    /// </summary>
    public int? MarketOutcomeId { get; set; }

    /// <summary>
    /// Legacy binary outcome (Yes/No) - kept for backward compatibility
    /// </summary>
    public Outcome? Outcome { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Shares { get; set; }

    [Required]
    [Range(0.01, 0.99)]
    public decimal Price { get; set; }

    [Required]
    public OrderType OrderType { get; set; }

    public decimal TotalCost { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Market Market { get; set; } = null!;
    public User User { get; set; } = null!;
    public MarketOutcome? MarketOutcome { get; set; }
}
