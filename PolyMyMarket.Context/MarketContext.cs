using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Models;

namespace PolyMyMarket.Context;

public class MarketContext : DbContext
{
    public MarketContext(DbContextOptions<MarketContext> options) : base(options)
    {
    }

    // DbSet properties
    public DbSet<Market> Markets { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Market configuration
        modelBuilder.Entity<Market>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
            entity.Property(m => m.Description).IsRequired().HasMaxLength(2000);
            entity.Property(m => m.Category).HasMaxLength(100);
            entity.Property(m => m.InitialLiquidity).HasPrecision(18, 2);
            entity.Property(m => m.YesPool).HasPrecision(18, 2);
            entity.Property(m => m.NoPool).HasPrecision(18, 2);

            entity.HasMany(m => m.Orders)
                .WithOne(o => o.Market)
                .HasForeignKey(o => o.MarketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(m => m.Positions)
                .WithOne(p => p.Market)
                .HasForeignKey(p => p.MarketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Shares).HasPrecision(18, 4);
            entity.Property(o => o.Price).HasPrecision(18, 4);
            entity.Property(o => o.TotalCost).HasPrecision(18, 2);

            entity.HasOne(o => o.Market)
                .WithMany(m => m.Orders)
                .HasForeignKey(o => o.MarketId);

            entity.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            entity.HasIndex(o => o.MarketId);
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.Timestamp);
        });

        // Position configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.YesShares).HasPrecision(18, 4);
            entity.Property(p => p.NoShares).HasPrecision(18, 4);
            entity.Property(p => p.AveragePriceYes).HasPrecision(18, 4);
            entity.Property(p => p.AveragePriceNo).HasPrecision(18, 4);
            entity.Property(p => p.TotalInvestedYes).HasPrecision(18, 2);
            entity.Property(p => p.TotalInvestedNo).HasPrecision(18, 2);

            entity.HasOne(p => p.Market)
                .WithMany(m => m.Positions)
                .HasForeignKey(p => p.MarketId);

            entity.HasOne(p => p.User)
                .WithMany(u => u.Positions)
                .HasForeignKey(p => p.UserId);

            // Unique constraint: one position per user per market
            entity.HasIndex(p => new { p.UserId, p.MarketId }).IsUnique();
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Balance).HasPrecision(18, 2);

            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.Positions)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed test user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "Demo User",
                Email = "demo@polymarket.com",
                Balance = 10000m,
                CreatedDate = DateTime.UtcNow
            }
        );

        // Seed markets
        modelBuilder.Entity<Market>().HasData(
            new Market
            {
                Id = 1,
                Title = "Will Bitcoin reach $100,000 by end of 2025?",
                Description = "This market resolves to Yes if Bitcoin (BTC) reaches or exceeds $100,000 USD at any point before December 31, 2025 23:59:59 UTC. Otherwise resolves to No.",
                Category = "Cryptocurrency",
                CreatedDate = DateTime.UtcNow,
                EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MarketStatus.Active,
                InitialLiquidity = 1000m,
                YesPool = 500m,
                NoPool = 500m
            },
            new Market
            {
                Id = 2,
                Title = "Will AI pass the Turing Test in 2025?",
                Description = "This market resolves to Yes if a credible AI system is widely recognized as passing the Turing Test by December 31, 2025. The determination will be based on mainstream media and academic consensus.",
                Category = "Technology",
                CreatedDate = DateTime.UtcNow,
                EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MarketStatus.Active,
                InitialLiquidity = 1000m,
                YesPool = 500m,
                NoPool = 500m
            },
            new Market
            {
                Id = 3,
                Title = "Will there be a recession in 2025?",
                Description = "This market resolves to Yes if the United States enters a recession (defined as two consecutive quarters of negative GDP growth) at any point during 2025.",
                Category = "Economics",
                CreatedDate = DateTime.UtcNow,
                EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MarketStatus.Active,
                InitialLiquidity = 1000m,
                YesPool = 500m,
                NoPool = 500m
            },
            new Market
            {
                Id = 4,
                Title = "Will SpaceX land humans on Mars by 2030?",
                Description = "This market resolves to Yes if SpaceX successfully lands human astronauts on the surface of Mars before December 31, 2030.",
                Category = "Space",
                CreatedDate = DateTime.UtcNow,
                EndDate = new DateTime(2030, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MarketStatus.Active,
                InitialLiquidity = 1000m,
                YesPool = 500m,
                NoPool = 500m
            },
            new Market
            {
                Id = 5,
                Title = "Will a major tech company announce a quantum computer breakthrough in 2025?",
                Description = "This market resolves to Yes if Google, IBM, Microsoft, or another major tech company announces a significant quantum computing breakthrough in 2025 that is covered by mainstream tech media.",
                Category = "Technology",
                CreatedDate = DateTime.UtcNow,
                EndDate = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                Status = MarketStatus.Active,
                InitialLiquidity = 1000m,
                YesPool = 500m,
                NoPool = 500m
            }
        );
    }
}

