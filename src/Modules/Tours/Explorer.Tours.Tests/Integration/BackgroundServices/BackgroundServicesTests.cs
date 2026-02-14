using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Explorer.Tours.Tests.Integration.BackgroundServices;

[Collection("Sequential")]
public class BackgroundServiceTests : BaseToursIntegrationTest
{
    public BackgroundServiceTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Tour_reminder_sends_email_48_hours_before_tour()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Create a tour scheduled 48 hours from now
        var tourDate = DateTime.UtcNow.AddHours(48);
        var tour = new Tour(
            11,
            "Reminder Test Tour",
            "A tour to test reminder functionality",
            2,
            1,
            150,
            tourDate,
            TourState.COMPLETE
        );
        dbContext.Tours.Add(tour);
        dbContext.SaveChanges();

        // Create a purchase for this tour
        var purchase = new TourPurchase(21, new List<long> { tour.Id }, 150, 0);
        dbContext.TourPurchases.Add(purchase);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        // Verify the purchase exists and ReminderSent is false
        var storedPurchase = dbContext.TourPurchases.FirstOrDefault(p => p.TouristId == 21);
        storedPurchase.ShouldNotBeNull();
        storedPurchase.ReminderSent.ShouldBeFalse();

        // Send the reminder (simulating what the background service would do)
        var reminderData = new TourReminderEmailData
        {
            TourName = tour.Name,
            TourDate = tour.Date,
            TourDescription = tour.Description,
            KeyPoints = new List<string>()
        };

        var result = emailService.SendTourReminderAsync(21, reminderData).Result;

        // Verify email was sent successfully
        result.IsSuccess.ShouldBeTrue();

        // Mark reminder as sent (simulating what the background service would do)
        storedPurchase.MarkReminderAsSent();
        dbContext.TourPurchases.Update(storedPurchase);
        dbContext.SaveChanges();

        // Verify ReminderSent flag is now true
        dbContext.ChangeTracker.Clear();
        var updatedPurchase = dbContext.TourPurchases.FirstOrDefault(p => p.TouristId == 21);
        updatedPurchase.ShouldNotBeNull();
        updatedPurchase.ReminderSent.ShouldBeTrue();
    }

    [Fact]
    public void Tour_recommendation_sends_emails_when_tour_is_published()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Create a new tour
        var tour = new Tour(
            11,
            "New Nature Tour",
            "Explore beautiful nature trails",
            2,
            1, // Category 1 = Nature
            200,
            DateTime.UtcNow.AddDays(30),
            TourState.COMPLETE
        );
        dbContext.Tours.Add(tour);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        // Send tour recommendation (simulating what happens when a tour is published)
        var recommendationData = new TourRecommendationEmailData
        {
            TourName = tour.Name,
            TourDescription = tour.Description,
            TourCategory = tour.Category,
            TourDate = tour.Date,
            TourPrice = tour.Price
        };

        var result = emailService.SendTourRecommendationAsync(tour.Id, recommendationData).Result;

        result.IsSuccess.ShouldBeTrue();

    }
}
