using Explorer.Tours.API.Controllers.Guide;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Infrastructure.Database;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using DomainTourReplacementStatus = Explorer.Tours.Core.Domain.TourReplacementStatus;

namespace Explorer.Tours.Tests.Integration.TourManagement;

[Collection("Sequential")]
public class TourReplacementTests : BaseToursIntegrationTest
{
    public TourReplacementTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_requests_replacement_for_published_future_tour()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        // Create a published tour in the future
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));

        var request = new TourReplacementCreateDto { TourId = tour.Id };

        // Act
        var actionResult = controller.RequestReplacement(request);
        var replacement = ((ObjectResult)actionResult.Result).Value as TourReplacementDto;

        // Assert - Response
        replacement.ShouldNotBeNull();
        replacement.Id.ShouldNotBe(0);
        replacement.TourId.ShouldBe(tour.Id);
        replacement.OriginalGuideId.ShouldBe(-11);
        ((int)replacement.Status).ShouldBe((int)DomainTourReplacementStatus.PENDING);

        // Assert - Database
        dbContext.ChangeTracker.Clear();
        var storedReplacement = dbContext.TourReplacements.FirstOrDefault(r => r.TourId == tour.Id);
        storedReplacement.ShouldNotBeNull();
        storedReplacement.Status.ShouldBe(DomainTourReplacementStatus.PENDING);
    }

    [Fact]
    public void Fails_to_request_replacement_for_draft_tour()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        // Create a DRAFT tour
        var tour = CreateTestTour(dbContext, -11, TourState.DRAFT, DateTime.UtcNow.AddDays(30));
        var request = new TourReplacementCreateDto { TourId = tour.Id };

        // Act
        var actionResult = controller.RequestReplacement(request);
        var result = (ObjectResult)actionResult.Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_request_replacement_for_past_tour()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        // Create a published tour in the PAST
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(-10));
        var request = new TourReplacementCreateDto { TourId = tour.Id };

        // Act
        var actionResult = controller.RequestReplacement(request);
        var result = (ObjectResult)actionResult.Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_request_replacement_for_another_guides_tour()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();

        // Create tour owned by guide -11
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));

        // Try to request replacement as guide -12
        var controller = CreateController(scope, guideId: -12);
        var request = new TourReplacementCreateDto { TourId = tour.Id };

        // Act
        var actionResult = controller.RequestReplacement(request);
        var result = (ObjectResult)actionResult.Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_request_replacement_when_pending_request_already_exists()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));
        var request = new TourReplacementCreateDto { TourId = tour.Id };

        // Act - First request succeeds
        var firstActionResult = controller.RequestReplacement(request);
        var firstResult = firstActionResult.Result;

        // Act - Second request should fail
        var secondActionResult = controller.RequestReplacement(request);
        var secondResult = (ObjectResult)secondActionResult.Result;

        // Assert
        secondResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Successfully_cancels_pending_replacement_request()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        // Create tour and request replacement
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));
        var request = new TourReplacementCreateDto { TourId = tour.Id };
        var createActionResult = controller.RequestReplacement(request);
        var replacement = ((ObjectResult)createActionResult.Result).Value as TourReplacementDto;

        // Act - Cancel the replacement
        var cancelActionResult = controller.CancelReplacementRequest(replacement!.Id);
        var cancelResult = (ObjectResult)cancelActionResult;

        // Assert
        cancelResult.StatusCode.ShouldBe(200);

        // Verify in database
        dbContext.ChangeTracker.Clear();
        var storedReplacement = dbContext.TourReplacements.FirstOrDefault(r => r.Id == replacement.Id);
        storedReplacement.ShouldNotBeNull();
        storedReplacement.Status.ShouldBe(DomainTourReplacementStatus.CANCELLED);
        storedReplacement.CancelledAt.ShouldNotBeNull();
    }

    [Fact]
    public void Fails_to_cancel_another_guides_replacement_request()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();

        // Guide -11 creates replacement request
        var controllerGuide11 = CreateController(scope, guideId: -11);
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));
        var request = new TourReplacementCreateDto { TourId = tour.Id };
        var createActionResult = controllerGuide11.RequestReplacement(request);
        var replacement = ((ObjectResult)createActionResult.Result).Value as TourReplacementDto;

        // Act - Guide -12 tries to cancel it
        var controllerGuide12 = CreateController(scope, guideId: -12);
        var cancelActionResult = controllerGuide12.CancelReplacementRequest(replacement!.Id);
        var cancelResult = (ObjectResult)cancelActionResult;

        // Assert
        cancelResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_cancel_non_pending_replacement()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, guideId: -11);

        // Create and then cancel a replacement
        var tour = CreateTestTour(dbContext, -11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30));
        var request = new TourReplacementCreateDto { TourId = tour.Id };
        var createActionResult = controller.RequestReplacement(request);
        var replacement = ((ObjectResult)createActionResult.Result).Value as TourReplacementDto;

        var firstCancelActionResult = controller.CancelReplacementRequest(replacement!.Id);
        var firstCancelResult = firstCancelActionResult;

        // Act - Try to cancel again
        var secondCancelActionResult = controller.CancelReplacementRequest(replacement.Id);
        var result = (ObjectResult)secondCancelActionResult;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    private static TourReplacementController CreateController(IServiceScope scope, long guideId)
    {
        var controller = new TourReplacementController(
            scope.ServiceProvider.GetRequiredService<ITourReplacementService>());

        controller.ControllerContext = BuildContext(guideId.ToString());
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

    private static Tour CreateTestTour(ToursContext dbContext, long authorId, TourState state, DateTime date)
    {
        var tour = new Tour(
            authorId,
            "Test Tour for Replacement",
            "A tour for testing replacement functionality",
            3,
            2,
            1000,
            date,
            state
        );

        dbContext.Tours.Add(tour);
        dbContext.SaveChanges();
        return tour;
    }
}