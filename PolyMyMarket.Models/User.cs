using System.ComponentModel.DataAnnotations;

namespace PolyMyMarket.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    public decimal Balance { get; set; } = 10000m; // Starting balance

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
}
