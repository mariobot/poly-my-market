using System.ComponentModel.DataAnnotations;

namespace PolyMyMarket.Models;

public class Market
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime EndDate { get; set; }

    public DateTime? ResolutionDate { get; set; }

    public MarketStatus Status { get; set; } = MarketStatus.Active;

    /// <summary>
    /// Type of market: Binary (Yes/No) or MultiOutcome (elections, etc.)
    /// </summary>
    public MarketType MarketType { get; set; } = MarketType.Binary;

    public bool? ResolvedOutcome { get; set; } // true = Yes, false = No, null = not resolved (legacy for binary markets)

    public decimal InitialLiquidity { get; set; } = 1000m;

    // Legacy binary market properties - kept for backward compatibility
    public decimal YesPool { get; set; } = 500m;
    public decimal NoPool { get; set; } = 500m;

    // Navigation properties
    public ICollection<MarketOutcome> Outcomes { get; set; } = new List<MarketOutcome>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Position> Positions { get; set; } = new List<Position>(); // Legacy binary positions
}
