// src/Modules/Tours/Explorer.Tours.Infrastructure/Database/ToursContext.cs
using Explorer.Tours.Core.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Explorer.Tours.Infrastructure.Database;

public class ToursContext : DbContext
{
    public DbSet<Equipment> Equipment { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<KeyPoint> KeyPoints { get; set; }

    // Purchase-related entities
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<TourPurchase> TourPurchases { get; set; }
    public DbSet<BonusPoints> BonusPoints { get; set; }
    public DbSet<BonusTransaction> BonusTransactions { get; set; }

    public ToursContext(DbContextOptions<ToursContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tours");

        ConfigureTour(modelBuilder);
        ConfigureShoppingCart(modelBuilder);
        ConfigureTourPurchase(modelBuilder);
        ConfigureBonusPoints(modelBuilder);
        ConfigureBonusTransaction(modelBuilder);
    }

    private static void ConfigureTour(ModelBuilder modelBuilder)
    {
        // Existing tour configuration - keeping it as is
    }

    private static void ConfigureShoppingCart(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingCart>()
            .HasKey(sc => sc.Id);

        // Convert List<long> to JSON for database storage
        modelBuilder.Entity<ShoppingCart>()
            .Property(sc => sc.TourIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions)null) ?? new List<long>()
            )
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        modelBuilder.Entity<ShoppingCart>()
            .HasIndex(sc => sc.TouristId)
            .IsUnique(); // One cart per tourist
    }

    private static void ConfigureTourPurchase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourPurchase>()
            .HasKey(tp => tp.Id);

        // Convert List<long> to JSON for database storage
        modelBuilder.Entity<TourPurchase>()
            .Property(tp => tp.TourIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions)null) ?? new List<long>()
            )
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // Configure decimal precision for money values
        modelBuilder.Entity<TourPurchase>()
            .Property(tp => tp.TotalAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<TourPurchase>()
            .Property(tp => tp.BonusPointsUsed)
            .HasPrecision(10, 2);

        modelBuilder.Entity<TourPurchase>()
            .Property(tp => tp.FinalAmount)
            .HasPrecision(10, 2);

        // Index for performance
        modelBuilder.Entity<TourPurchase>()
            .HasIndex(tp => tp.TouristId);

        modelBuilder.Entity<TourPurchase>()
            .HasIndex(tp => tp.PurchaseDate);
    }

    private static void ConfigureBonusPoints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BonusPoints>()
            .HasKey(bp => bp.Id);

        // Configure decimal precision
        modelBuilder.Entity<BonusPoints>()
            .Property(bp => bp.AvailablePoints)
            .HasPrecision(10, 2);

        // One bonus points record per tourist
        modelBuilder.Entity<BonusPoints>()
            .HasIndex(bp => bp.TouristId)
            .IsUnique();
    }

    private static void ConfigureBonusTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BonusTransaction>()
            .HasKey(bt => bt.Id);

        // Configure decimal precision
        modelBuilder.Entity<BonusTransaction>()
            .Property(bt => bt.Amount)
            .HasPrecision(10, 2);

        // Required fields
        modelBuilder.Entity<BonusTransaction>()
            .Property(bt => bt.Description)
            .IsRequired()
            .HasMaxLength(500);

        // Indexes for performance
        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.TouristId);

        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.CreatedAt);

        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.Type);
    }
}