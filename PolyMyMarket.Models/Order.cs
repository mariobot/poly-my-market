using System.ComponentModel.DataAnnotations;

namespace PolyMyMarket.Models;

public class Order
{
    public int Id { get; set; }

    [Required]
    public int MarketId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public Outcome Outcome { get; set; }

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
}
