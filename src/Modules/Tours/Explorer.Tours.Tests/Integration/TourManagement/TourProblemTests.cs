using Explorer.API.Controllers.Administrator;
using Explorer.API.Controllers.Author;
using Explorer.API.Controllers.Tourist;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Explorer.Tours.Tests.Integration.TourManagement;

[Collection("Sequential")]
public class TourProblemTests : BaseToursIntegrationTest
{
    public TourProblemTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Tourist_successfully_reports_problem_on_purchased_tour()
    {
        // Setup phase
        long tourId;
        long purchaseId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create a tour
            var tour = new Tour(
                11,
                "Tour with Problem",
                "A tour that will have a problem reported",
                3,
                1,
                150,
                DateTime.UtcNow.AddDays(30),
                TourState.COMPLETE
            );
            dbContext.Tours.Add(tour);
            dbContext.SaveChanges();
            tourId = tour.Id;

            // Create a purchase for this tour
            var purchase = new TourPurchase(33, new List<long> { tourId }, 150, 0);
            dbContext.TourPurchases.Add(purchase);
            dbContext.SaveChanges();
            purchaseId = purchase.Id;

            dbContext.ChangeTracker.Clear();
        }

        // Test phase - tourist reports problem
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateTouristController(testScope, touristId: 33);

            var problemDto = new CreateTourProblemDto
            {
                TourId = tourId,
                Title = "Tour guide was unprofessional",
                Description = "The tour guide arrived late and was rude to participants."
            };

            var result = ((ObjectResult)controller.ReportProblem(problemDto).Result)?.Value as TourProblemDto;

            result.ShouldNotBeNull();
            result.TourId.ShouldBe(tourId);
            result.TouristId.ShouldBe(33);
            result.Title.ShouldBe("Tour guide was unprofessional");
            result.Description.ShouldBe("The tour guide arrived late and was rude to participants.");
            result.Status.ShouldBe((int)TourProblemStatus.Pending);
        }

        // Verification phase
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedProblem = dbContext.TourProblems.FirstOrDefault(p => p.TourId == tourId);
            storedProblem.ShouldNotBeNull();
            storedProblem.Status.ShouldBe(TourProblemStatus.Pending);
            storedProblem.TouristId.ShouldBe(33);
        }
    }

    [Fact]
    public void Guide_successfully_marks_problem_as_resolved()
    {
        // Setup phase
        long tourId;
        long problemId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create a tour by guide 11
            var tour = new Tour(
                11,
                "Tour for Problem Resolution",
                "Guide will resolve problem",
                2,
                2,
                100,
                DateTime.UtcNow.AddDays(20),
                TourState.COMPLETE
            );
            dbContext.Tours.Add(tour);
            dbContext.SaveChanges();
            tourId = tour.Id;

            // Create a problem on this tour
            var problem = new TourProblem(tourId, 34, "Issue Title", "Issue Description");
            dbContext.TourProblems.Add(problem);
            dbContext.SaveChanges();
            problemId = problem.Id;

            dbContext.ChangeTracker.Clear();
        }

        // Test phase - guide marks as resolved
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateGuideController(testScope, guideId: 11);

            var result = ((ObjectResult)controller.MarkAsResolved(problemId).Result)?.Value as TourProblemDto;

            result.ShouldNotBeNull();
            result.Id.ShouldBe(problemId);
            result.Status.ShouldBe((int)TourProblemStatus.Resolved);
            result.ResolvedAt.ShouldNotBeNull();
        }

        // Verification phase
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedProblem = dbContext.TourProblems.FirstOrDefault(p => p.Id == problemId);
            storedProblem.ShouldNotBeNull();
            storedProblem.Status.ShouldBe(TourProblemStatus.Resolved);
            storedProblem.ResolvedAt.ShouldNotBeNull();
        }
    }

    [Fact]
    public void Guide_sends_problem_to_administrator_for_review()
    {
        // Setup phase
        long tourId;
        long problemId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create a tour by guide 11
            var tour = new Tour(
                11,
                "Tour with Invalid Problem",
                "Guide thinks problem is invalid",
                2,
                1,
                120,
                DateTime.UtcNow.AddDays(15),
                TourState.COMPLETE
            );
            dbContext.Tours.Add(tour);
            dbContext.SaveChanges();
            tourId = tour.Id;

            // Create a problem
            var problem = new TourProblem(tourId, 35, "Invalid Complaint", "Tourist is complaining unfairly");
            dbContext.TourProblems.Add(problem);
            dbContext.SaveChanges();
            problemId = problem.Id;

            dbContext.ChangeTracker.Clear();
        }

        // Test phase - guide sends to admin
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateGuideController(testScope, guideId: 11);

            var result = ((ObjectResult)controller.SendToAdministrator(problemId).Result)?.Value as TourProblemDto;

            result.ShouldNotBeNull();
            result.Id.ShouldBe(problemId);
            result.Status.ShouldBe((int)TourProblemStatus.UnderReview);
            result.ReviewRequestedAt.ShouldNotBeNull();
        }

        // Verification phase
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedProblem = dbContext.TourProblems.FirstOrDefault(p => p.Id == problemId);
            storedProblem.ShouldNotBeNull();
            storedProblem.Status.ShouldBe(TourProblemStatus.UnderReview);
            storedProblem.ReviewRequestedAt.ShouldNotBeNull();
        }
    }

    [Fact]
    public void Administrator_returns_problem_to_guide()
    {
        // Setup phase
        long tourId;
        long problemId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create a tour
            var tour = new Tour(
                11,
                "Tour for Admin Review",
                "Problem under admin review",
                3,
                2,
                180,
                DateTime.UtcNow.AddDays(25),
                TourState.COMPLETE
            );
            dbContext.Tours.Add(tour);
            dbContext.SaveChanges();
            tourId = tour.Id;

            // Create a problem and send to admin
            var problem = new TourProblem(tourId, 36, "Problem Title", "Problem Description");
            problem.SendToAdministrator();
            dbContext.TourProblems.Add(problem);
            dbContext.SaveChanges();
            problemId = problem.Id;

            dbContext.ChangeTracker.Clear();
        }

        // Test phase - admin returns to guide
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateAdminController(testScope);

            var result = ((ObjectResult)controller.ReturnToGuide(problemId).Result)?.Value as TourProblemDto;

            result.ShouldNotBeNull();
            result.Id.ShouldBe(problemId);
            result.Status.ShouldBe((int)TourProblemStatus.Pending);
        }

        // Verification phase
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedProblem = dbContext.TourProblems.FirstOrDefault(p => p.Id == problemId);
            storedProblem.ShouldNotBeNull();
            storedProblem.Status.ShouldBe(TourProblemStatus.Pending);
        }
    }

    [Fact]
    public void Administrator_rejects_invalid_problem()
    {
        // Setup phase
        long tourId;
        long problemId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create a tour
            var tour = new Tour(
                11,
                "Tour for Problem Rejection",
                "Problem will be rejected",
                2,
                3,
                200,
                DateTime.UtcNow.AddDays(35),
                TourState.COMPLETE
            );
            dbContext.Tours.Add(tour);
            dbContext.SaveChanges();
            tourId = tour.Id;

            // Create a problem under review
            var problem = new TourProblem(tourId, 37, "Invalid Problem", "This is not a real problem");
            problem.SendToAdministrator();
            dbContext.TourProblems.Add(problem);
            dbContext.SaveChanges();
            problemId = problem.Id;

            dbContext.ChangeTracker.Clear();
        }

        // Test phase - admin rejects
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateAdminController(testScope);

            var result = ((ObjectResult)controller.RejectProblem(problemId).Result)?.Value as TourProblemDto;

            result.ShouldNotBeNull();
            result.Id.ShouldBe(problemId);
            result.Status.ShouldBe((int)TourProblemStatus.Rejected);
            result.RejectedAt.ShouldNotBeNull();
        }

        // Verification phase
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedProblem = dbContext.TourProblems.FirstOrDefault(p => p.Id == problemId);
            storedProblem.ShouldNotBeNull();
            storedProblem.Status.ShouldBe(TourProblemStatus.Rejected);
            storedProblem.RejectedAt.ShouldNotBeNull();
        }
    }

    private static TourProblemController CreateTouristController(IServiceScope scope, long touristId)
    {
        var controller = new TourProblemController(
            scope.ServiceProvider.GetRequiredService<ITourProblemService>());

        controller.ControllerContext = BuildContext(touristId.ToString());
        return controller;
    }

    private static GuideTourProblemController CreateGuideController(IServiceScope scope, long guideId)
    {
        var controller = new GuideTourProblemController(
            scope.ServiceProvider.GetRequiredService<ITourProblemService>());

        controller.ControllerContext = BuildContext(guideId.ToString());
        return controller;
    }

    private static AdminTourProblemController CreateAdminController(IServiceScope scope)
    {
        var controller = new AdminTourProblemController(
            scope.ServiceProvider.GetRequiredService<ITourProblemService>());

        controller.ControllerContext = BuildContext("1"); // Admin ID
        return controller;
    }

    private static ControllerContext BuildContext(string id)
    {
        return new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim("id", id),
                        new System.Security.Claims.Claim("personId", id)
                    }))
            }
        };
    }
}