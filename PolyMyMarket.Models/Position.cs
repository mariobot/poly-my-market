using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PolyMyMarket.Models;

public class Position
{
    public int Id { get; set; }

    [Required]
    public int MarketId { get; set; }

    [Required]
    public int UserId { get; set; }

    public decimal YesShares { get; set; } = 0m;

    public decimal NoShares { get; set; } = 0m;

    public decimal AveragePriceYes { get; set; } = 0m;

    public decimal AveragePriceNo { get; set; } = 0m;

    public decimal TotalInvestedYes { get; set; } = 0m;

    public decimal TotalInvestedNo { get; set; } = 0m;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Market Market { get; set; } = null!;
    public User User { get; set; } = null!;

    // Calculated properties (not stored in DB)
    [NotMapped]
    public decimal TotalShares => YesShares + NoShares;

    [NotMapped]
    public decimal TotalInvested => TotalInvestedYes + TotalInvestedNo;

    public decimal CalculateCurrentValue(decimal currentYesPrice, decimal currentNoPrice)
    {
        return (YesShares * currentYesPrice) + (NoShares * currentNoPrice);
    }

    public decimal CalculateProfitLoss(decimal currentYesPrice, decimal currentNoPrice)
    {
        return CalculateCurrentValue(currentYesPrice, currentNoPrice) - TotalInvested;
    }
}
