using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Explorer.API.Controllers.Author;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Administration;
using Explorer.Tours.Infrastructure.Database;
using Explorer.Tours.Core.Domain;

namespace Explorer.Tours.Tests.Integration.TourManagement;

[Collection("Sequential")]
public class KeyPointCreationTests : BaseToursIntegrationTest
{
    public KeyPointCreationTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_creates_key_point_with_valid_data()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();

        // First create a tour
        var tour = CreateTestTour(dbContext, -11);

        var controller = CreateController(scope);
        var keyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "Petrovaradin Fortress",
            Description = "Historic fortress overlooking the Danube river with beautiful views",
            Latitude = 45.2517,
            Longitude = 19.8661,
            Order = 1
        };

        // Act
        var result = controller.Create(keyPoint).Result;
        var createdKeyPoint = ((ObjectResult)result).Value as KeyPointDto;

        // Assert - Response
        createdKeyPoint.ShouldNotBeNull();
        createdKeyPoint.Id.ShouldNotBe(0);
        createdKeyPoint.Name.ShouldBe(keyPoint.Name);
        createdKeyPoint.TourId.ShouldBe(tour.Id);
        createdKeyPoint.Latitude.ShouldBe(keyPoint.Latitude);
        createdKeyPoint.Longitude.ShouldBe(keyPoint.Longitude);

        // Assert - Database
        dbContext.ChangeTracker.Clear();
        var storedKeyPoint = dbContext.KeyPoints.FirstOrDefault(kp => kp.Name == keyPoint.Name);
        storedKeyPoint.ShouldNotBeNull();
        storedKeyPoint.TourId.ShouldBe(tour.Id);
        storedKeyPoint.Order.ShouldBe(1);
    }

    [Fact]
    public void Successfully_creates_key_point_with_image_url()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var tour = CreateTestTour(dbContext, -11);
        var controller = CreateController(scope);

        var keyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "Cathedral",
            Description = "Beautiful cathedral in the city center",
            Latitude = 45.2550,
            Longitude = 19.8450,
            ImageUrl = "https://example.com/cathedral.jpg",
            Order = 2
        };

        // Act
        var result = controller.Create(keyPoint).Result;
        var createdKeyPoint = ((ObjectResult)result).Value as KeyPointDto;

        // Assert
        createdKeyPoint.ShouldNotBeNull();
        createdKeyPoint.ImageUrl.ShouldBe(keyPoint.ImageUrl);
    }

    [Fact]
    public void Fails_to_create_key_point_with_empty_name()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var tour = CreateTestTour(dbContext, -11);
        var controller = CreateController(scope);

        var keyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "", // Empty name
            Description = "Valid description",
            Latitude = 45.2550,
            Longitude = 19.8450,
            Order = 1
        };

        // Act
        var result = (ObjectResult)controller.Create(keyPoint).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_key_point_with_invalid_latitude()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var tour = CreateTestTour(dbContext, -11);
        var controller = CreateController(scope);

        var keyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "Test Point",
            Description = "Valid description",
            Latitude = 95.0, // Invalid - must be between -90 and 90
            Longitude = 19.8450,
            Order = 1
        };

        // Act
        var result = (ObjectResult)controller.Create(keyPoint).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_key_point_with_invalid_longitude()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var tour = CreateTestTour(dbContext, -11);
        var controller = CreateController(scope);

        var keyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "Test Point",
            Description = "Valid description",
            Latitude = 45.2550,
            Longitude = 200.0, // Invalid - must be between -180 and 180
            Order = 1
        };

        // Act
        var result = (ObjectResult)controller.Create(keyPoint).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_key_point_for_nonexistent_tour()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);

        var keyPoint = new KeyPointDto
        {
            TourId = 99999, // Non-existent tour
            Name = "Test Point",
            Description = "Valid description",
            Latitude = 45.2550,
            Longitude = 19.8450,
            Order = 1
        };

        // Act
        var result = (ObjectResult)controller.Create(keyPoint).Result;

        // Assert
        result.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void Automatically_sets_order_when_not_provided()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var tour = CreateTestTour(dbContext, -11);
        var controller = CreateController(scope);

        // Create first key point with explicit order
        var firstKeyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "First Point",
            Description = "First key point",
            Latitude = 45.2550,
            Longitude = 19.8450,
            Order = 1
        };
        var _ = controller.Create(firstKeyPoint).Result;

        // Create second key point without order (should be auto-set to 2)
        var secondKeyPoint = new KeyPointDto
        {
            TourId = tour.Id,
            Name = "Second Point",
            Description = "Second key point",
            Latitude = 45.2560,
            Longitude = 19.8460,
            Order = 0 // Not set
        };

        // Act
        var result = controller.Create(secondKeyPoint).Result;
        var createdKeyPoint = ((ObjectResult)result).Value as KeyPointDto;

        // Assert
        createdKeyPoint.ShouldNotBeNull();
        createdKeyPoint.Order.ShouldBe(2); // Should auto-increment
    }

    private static KeyPointsController CreateController(IServiceScope scope)
    {
        return new KeyPointsController(scope.ServiceProvider.GetRequiredService<IKeyPointService>());
    }

    private static Tour CreateTestTour(ToursContext dbContext, long authorId)
    {
        var tour = new Tour(
            authorId,
            "Test Tour for KeyPoints",
            "A tour for testing key points",
            3,
            2,
            1000,
            DateTime.UtcNow.AddDays(30),
            TourState.DRAFT
        );

        dbContext.Tours.Add(tour);
        dbContext.SaveChanges();
        return tour;
    }
}