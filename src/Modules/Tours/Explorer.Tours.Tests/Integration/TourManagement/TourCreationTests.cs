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
public class TourCreationTests : BaseToursIntegrationTest
{
    public TourCreationTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_creates_tour_with_valid_data()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, authorId: -11); // autor1 from test data

        var tour = new TourDto
        {
            Name = "Mountain Adventure",
            Description = "Explore the beautiful mountains with experienced guides",
            Difficulty = 3,
            Category = 4,
            Price = 1500,
            Date = DateTime.UtcNow.AddDays(30),
            State = (int)TourState.DRAFT
        };

        // Act
        var result = controller.Create(tour).Result;
        var createdTour = ((ObjectResult)result).Value as TourDto;

        // Assert - Response
        createdTour.ShouldNotBeNull();
        createdTour.Id.ShouldNotBe(0);
        createdTour.Name.ShouldBe(tour.Name);
        createdTour.Description.ShouldBe(tour.Description);
        createdTour.AuthorId.ShouldBe(-11); // Set from JWT claim

        // Assert - Database
        dbContext.ChangeTracker.Clear();
        var storedTour = dbContext.Tours.FirstOrDefault(t => t.Name == tour.Name);
        storedTour.ShouldNotBeNull();
        storedTour.AuthorId.ShouldBe(-11);
        storedTour.State.ShouldBe(TourState.DRAFT);
    }

    [Fact]
    public void Fails_to_create_tour_with_empty_name()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope, authorId: -11);

        var tour = new TourDto
        {
            Name = "", // Empty name
            Description = "Valid description",
            Difficulty = 3,
            Category = 2,
            Price = 1000,
            Date = DateTime.UtcNow.AddDays(10),
            State = (int)TourState.DRAFT
        };

        // Act
        var result = (ObjectResult)controller.Create(tour).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_tour_with_invalid_difficulty()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope, authorId: -11);

        var tour = new TourDto
        {
            Name = "Test Tour",
            Description = "Valid description",
            Difficulty = 10, // Invalid - must be 1-5
            Category = 2,
            Price = 1000,
            Date = DateTime.UtcNow.AddDays(10),
            State = (int)TourState.DRAFT
        };

        // Act
        var result = (ObjectResult)controller.Create(tour).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_tour_with_negative_price()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope, authorId: -11);

        var tour = new TourDto
        {
            Name = "Test Tour",
            Description = "Valid description",
            Difficulty = 3,
            Category = 2,
            Price = -500, // Invalid - negative price
            Date = DateTime.UtcNow.AddDays(10),
            State = (int)TourState.DRAFT
        };

        // Act
        var result = (ObjectResult)controller.Create(tour).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_create_tour_with_past_date()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope, authorId: -11);

        var tour = new TourDto
        {
            Name = "Test Tour",
            Description = "Valid description",
            Difficulty = 3,
            Category = 2,
            Price = 1000,
            Date = DateTime.UtcNow.AddDays(-10), // Past date
            State = (int)TourState.DRAFT
        };

        // Act
        var result = (ObjectResult)controller.Create(tour).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    private static ToursController CreateController(IServiceScope scope, long authorId)
    {
        var controller = new ToursController(scope.ServiceProvider.GetRequiredService<ITourService>());

        // Mock JWT claims
        controller.ControllerContext = BuildContext(authorId.ToString());

        return controller;
    }

    private static Microsoft.AspNetCore.Mvc.ControllerContext BuildContext(string id)
    {
        return new Microsoft.AspNetCore.Mvc.ControllerContext()
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("id", id)
                }))
            }
        };
    }
}