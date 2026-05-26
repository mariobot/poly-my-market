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

    public bool? ResolvedOutcome { get; set; } // true = Yes, false = No, null = not resolved

    public decimal InitialLiquidity { get; set; } = 1000m;

    public decimal YesPool { get; set; } = 500m;

    public decimal NoPool { get; set; } = 500m;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
}
