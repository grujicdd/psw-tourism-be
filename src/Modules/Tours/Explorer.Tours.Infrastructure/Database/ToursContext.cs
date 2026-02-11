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
    public DbSet<TourReview> TourReviews { get; set; }

    // Tour Problem Report
    public DbSet<TourProblem> TourProblems { get; set; }
    public DbSet<TourReplacement> TourReplacements { get; set; }

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
        ConfigureTourReplacement(modelBuilder); // NEW!
    }

    private static void ConfigureTour(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>()
            .HasKey(t => t.Id);

        // Required fields
        modelBuilder.Entity<Tour>()
            .Property(t => t.Name)
            .IsRequired();

        modelBuilder.Entity<Tour>()
            .Property(t => t.Description)
            .IsRequired();

        modelBuilder.Entity<Tour>()
            .Property(t => t.AuthorId)
            .IsRequired();

        // Indexes for performance
        modelBuilder.Entity<Tour>()
            .HasIndex(t => t.AuthorId);

        modelBuilder.Entity<Tour>()
            .HasIndex(t => t.Date);

        modelBuilder.Entity<Tour>()
            .HasIndex(t => t.State);

        modelBuilder.Entity<Tour>()
            .HasIndex(t => t.Category);
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

    private static void ConfigureTourReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourReview>()
            .HasKey(tr => tr.Id);

        // Indexes for performance
        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TouristId);

        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TourId);

        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => tr.TourPurchaseId);

        // Composite unique index: one review per tour per purchase
        modelBuilder.Entity<TourReview>()
            .HasIndex(tr => new { tr.TourPurchaseId, tr.TourId })
            .IsUnique();

        // Optional: Configure comment max length
        modelBuilder.Entity<TourReview>()
            .Property(tr => tr.Comment)
            .HasMaxLength(1000);
    }

    private static void ConfigureTourProblem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourProblem>()
            .HasKey(tp => tp.Id);

        // Required fields
        modelBuilder.Entity<TourProblem>()
            .Property(tp => tp.Title)
            .IsRequired();

        modelBuilder.Entity<TourProblem>()
            .Property(tp => tp.Description)
            .IsRequired();

        // Indexes for performance
        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.TourId);

        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.TouristId);

        modelBuilder.Entity<TourProblem>()
            .HasIndex(tp => tp.Status);
    }

    private static void ConfigureTourReplacement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TourReplacement>()
            .HasKey(tr => tr.Id);

        // Indexes for performance and querying
        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => tr.TourId);

        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => tr.OriginalGuideId);

        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => tr.ReplacementGuideId);

        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => tr.Status);

        // For queries like "get pending replacements for specific tour"
        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => new { tr.TourId, tr.Status });

        // For queries like "get all pending replacements by guide"
        modelBuilder.Entity<TourReplacement>()
            .HasIndex(tr => new { tr.OriginalGuideId, tr.Status });
    }
}