using Explorer.API.Controllers.Tourist;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using PurchaseStatus = Explorer.Tours.Core.Domain.PurchaseStatus;

namespace Explorer.Tours.Tests.Integration.TourManagement;

[Collection("Sequential")]
public class TourPurchaseTests : BaseToursIntegrationTest
{
    public TourPurchaseTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_purchases_tours_from_cart()
    {
        // Setup phase
        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Create test tours
            var tour1 = CreateTestTour(dbContext, 11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30), price: 100);
            var tour2 = CreateTestTour(dbContext, 11, TourState.COMPLETE, DateTime.UtcNow.AddDays(45), price: 150);

            // Clear any existing data
            var existingCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 21);
            if (existingCart != null)
            {
                dbContext.ShoppingCarts.Remove(existingCart);
            }

            var existingPurchases = dbContext.TourPurchases.Where(p => p.TouristId == 21).ToList();
            foreach (var purchase in existingPurchases)
            {
                dbContext.TourPurchases.Remove(purchase);
            }

            dbContext.SaveChanges();
            dbContext.ChangeTracker.Clear();

            // Add tours to cart
            var cart = new ShoppingCart(21);
            cart.AddTour(tour1.Id);
            cart.AddTour(tour2.Id);
            dbContext.ShoppingCarts.Add(cart);
            dbContext.SaveChanges();
        }

        // Purchase phase - use a new scope
        TourPurchaseDto result;
        using (var purchaseScope = Factory.Services.CreateScope())
        {
            var controller = CreateController(purchaseScope, touristId: 21);

            // Process purchase with no bonus points
            result = ((ObjectResult)controller.ProcessPurchase(0).Result)?.Value as TourPurchaseDto;

            result.ShouldNotBeNull();
            result.TouristId.ShouldBe(21);
            result.TotalAmount.ShouldBe(250); // 100 + 150
            result.BonusPointsUsed.ShouldBe(0);
            result.FinalAmount.ShouldBe(250);
            ((int)result.Status).ShouldBe((int)PurchaseStatus.Completed);
        }

        // Verification phase - use another new scope
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            // Verify purchase was stored
            var storedPurchase = dbContext.TourPurchases.FirstOrDefault(p => p.TouristId == 21);
            storedPurchase.ShouldNotBeNull();
            storedPurchase.TourIds.Count.ShouldBe(2);
            storedPurchase.Status.ShouldBe(PurchaseStatus.Completed);

            // Verify cart was cleared
            var clearedCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 21);
            clearedCart.ShouldNotBeNull();
            clearedCart.TourIds.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void Successfully_purchases_with_bonus_points()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, touristId: 22);

        // Create test tour
        var tour = CreateTestTour(dbContext, 11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30), price: 200);

        // Clean up existing records
        var existingCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 22);
        if (existingCart != null)
        {
            dbContext.ShoppingCarts.Remove(existingCart);
        }
        var existingBonusPoints = dbContext.BonusPoints.FirstOrDefault(bp => bp.TouristId == 22);
        if (existingBonusPoints != null)
        {
            dbContext.BonusPoints.Remove(existingBonusPoints);
        }
        dbContext.SaveChanges();

        // Add tour to cart
        var cart = new ShoppingCart(22);
        cart.AddTour(tour.Id);
        dbContext.ShoppingCarts.Add(cart);

        // Create bonus points for tourist
        var bonusPoints = new BonusPoints(22);
        bonusPoints.AddPoints(50, "Test bonus");
        dbContext.BonusPoints.Add(bonusPoints);

        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        // Process purchase with 50 bonus points
        var result = ((ObjectResult)controller.ProcessPurchase(50).Result)?.Value as TourPurchaseDto;

        result.ShouldNotBeNull();
        result.TotalAmount.ShouldBe(200);
        result.BonusPointsUsed.ShouldBe(50);
        result.FinalAmount.ShouldBe(150); // 200 - 50

        // Verify bonus points were deducted
        dbContext.ChangeTracker.Clear();
        var updatedBonusPoints = dbContext.BonusPoints.FirstOrDefault(bp => bp.TouristId == 22);
        updatedBonusPoints.ShouldNotBeNull();
        updatedBonusPoints.AvailablePoints.ShouldBe(0); // 50 - 50
    }

    [Fact]
    public void Fails_to_purchase_with_empty_cart()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, touristId: 23);

        // Clean up existing cart
        var existingCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 23);
        if (existingCart != null)
        {
            dbContext.ShoppingCarts.Remove(existingCart);
            dbContext.SaveChanges();
        }

        // Create empty cart
        var cart = new ShoppingCart(23);
        dbContext.ShoppingCarts.Add(cart);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        var result = (ObjectResult)controller.ProcessPurchase(0).Result;

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_purchase_with_insufficient_bonus_points()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, touristId: 24);

        // Create test tour
        var tour = CreateTestTour(dbContext, 11, TourState.COMPLETE, DateTime.UtcNow.AddDays(30), price: 200);

        // Clean up existing records
        var existingCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 24);
        if (existingCart != null)
        {
            dbContext.ShoppingCarts.Remove(existingCart);
        }
        var existingBonusPoints = dbContext.BonusPoints.FirstOrDefault(bp => bp.TouristId == 24);
        if (existingBonusPoints != null)
        {
            dbContext.BonusPoints.Remove(existingBonusPoints);
        }
        dbContext.SaveChanges();

        // Add tour to cart
        var cart = new ShoppingCart(24);
        cart.AddTour(tour.Id);
        dbContext.ShoppingCarts.Add(cart);

        // Create bonus points for tourist (only 30 points)
        var bonusPoints = new BonusPoints(24);
        bonusPoints.AddPoints(30, "Test bonus");
        dbContext.BonusPoints.Add(bonusPoints);

        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        // Try to use 100 bonus points when only 30 available
        var result = (ObjectResult)controller.ProcessPurchase(100).Result;

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_purchase_unpublished_tour()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ToursContext>();
        var controller = CreateController(scope, touristId: 25);

        // Create draft tour
        var tour = CreateTestTour(dbContext, 11, TourState.DRAFT, DateTime.UtcNow.AddDays(30), price: 100);

        // Clean up existing cart
        var existingCart = dbContext.ShoppingCarts.FirstOrDefault(c => c.TouristId == 25);
        if (existingCart != null)
        {
            dbContext.ShoppingCarts.Remove(existingCart);
            dbContext.SaveChanges();
        }

        // Add to cart
        var cart = new ShoppingCart(25);
        cart.AddTour(tour.Id);
        dbContext.ShoppingCarts.Add(cart);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        var result = (ObjectResult)controller.ProcessPurchase(0).Result;

        result.ShouldNotBeNull();
        result.StatusCode.ShouldBe(400);
    }

    private static TourPurchaseController CreateController(IServiceScope scope, long touristId)
    {
        var controller = new TourPurchaseController(
            scope.ServiceProvider.GetRequiredService<ITourPurchaseService>());

        controller.ControllerContext = BuildContext(touristId.ToString());
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

    private static Tour CreateTestTour(ToursContext dbContext, long authorId, TourState state, DateTime date, int price)
    {
        var tour = new Tour(
            authorId,
            "Test Tour for Purchase",
            "A tour for testing purchase functionality",
            3,        // Difficulty
            2,        // Category
            price,    // Price
            date,
            state
        );

        dbContext.Tours.Add(tour);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();
        return tour;
    }
}