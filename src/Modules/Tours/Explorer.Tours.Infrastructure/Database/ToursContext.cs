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
    public DbSet<TourReview> TourReviews { get; set; }

    // Problem reporting
    public DbSet<TourProblem> TourProblems { get; set; }

    public ToursContext(DbContextOptions<ToursContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tours");

        ConfigureTour(modelBuilder);
        ConfigureShoppingCart(modelBuilder);
        ConfigureTourPurchase(modelBuilder);
        ConfigureBonusPoints(modelBuilder);
        ConfigureBonusTransaction(modelBuilder);
        ConfigureTourReview(modelBuilder);
        ConfigureTourProblem(modelBuilder);
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
            .HasColumnType("jsonb");

        modelBuilder.Entity<TourPurchase>()
            .HasIndex(tp => tp.TouristId);

        modelBuilder.Entity<TourPurchase>()
            .HasIndex(tp => tp.PurchaseDate);
    }

    private static void ConfigureBonusPoints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BonusPoints>()
            .HasKey(bp => bp.Id);

        modelBuilder.Entity<BonusPoints>()
            .Property(bp => bp.AvailablePoints)
            .HasPrecision(10, 2);

        modelBuilder.Entity<BonusPoints>()
            .HasIndex(bp => bp.TouristId)
            .IsUnique(); // One bonus points record per tourist
    }

    private static void ConfigureBonusTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BonusTransaction>()
            .HasKey(bt => bt.Id);

        modelBuilder.Entity<BonusTransaction>()
            .Property(bt => bt.Amount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.TouristId);

        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.Type);

        modelBuilder.Entity<BonusTransaction>()
            .HasIndex(bt => bt.CreatedAt);
    }

    private static void ConfigureTourReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourReview>()
            .HasKey(tr => tr.Id);

        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TourId);

        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TouristId);

        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TourPurchaseId);
    }

    private static void ConfigureTourProblem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourProblem>()
            .HasKey(tp => tp.Id);

        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.TourId);

        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.TouristId);

        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.Status);
    }
}