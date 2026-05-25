using Microsoft.EntityFrameworkCore;
using PolyMyMarket.Models;

namespace PolyMyMarket.Context;

public class AppContext : DbContext
{
    public AppContext(DbContextOptions<AppContext> options) : base(options)
    {
    }

    // Add your DbSet properties here as you create entities
    // Example:
    // public DbSet<Product> Products { get; set; }
    // public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure your entity relationships and constraints here
        // Example:
        // modelBuilder.Entity<Product>()
        //     .HasOne(p => p.Category)
        //     .WithMany(c => c.Products)
        //     .HasForeignKey(p => p.CategoryId);
    }
}
