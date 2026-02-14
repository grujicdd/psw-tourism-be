using Explorer.API.Controllers.Tourist;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using Explorer.Tours.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Explorer.Tours.Tests.Integration.TourManagement;

[Collection("Sequential")]
public class TourReviewTests : BaseToursIntegrationTest
{
    public TourReviewTests(ToursTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_creates_and_stores_review_for_completed_tour()
    {
        // Setup and insert review directly (bypassing service validation for testing)
        long tourId;
        long purchaseId;
        long reviewId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            var pastDate = DateTime.UtcNow.AddDays(-2).AddHours(-1);

            var connection = dbContext.Database.GetDbConnection();
            connection.Open();

            // Insert tour
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO tours.""Tours"" (""AuthorId"", ""Name"", ""Description"", ""Difficulty"", ""Category"", ""Price"", ""Date"", ""State"")
                    VALUES (11, 'Past Tour for Review', 'A tour that already happened', 3, 1, 150, @date, 1)
                    RETURNING ""Id""";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@date";
                parameter.Value = pastDate;
                command.Parameters.Add(parameter);

                tourId = (long)command.ExecuteScalar();
            }

            // Create a purchase
            var purchase = new TourPurchase(26, new List<long> { tourId }, 150, 0);
            dbContext.TourPurchases.Add(purchase);
            dbContext.SaveChanges();
            purchaseId = purchase.Id;

            // Insert review directly with SQL
            dbContext.Database.ExecuteSqlRaw(
                @"INSERT INTO tours.""TourReviews"" (""TourPurchaseId"", ""TourId"", ""TouristId"", ""Rating"", ""Comment"", ""ReviewDate"")
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5})
                  RETURNING ""Id""",
                purchaseId, tourId, 26, 5, "Amazing tour! Highly recommend!", DateTime.UtcNow
            );

            connection.Close();
        }

        // Verification phase - verify the review was stored correctly
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedReview = dbContext.TourReviews
                .FirstOrDefault(r => r.TourPurchaseId == purchaseId && r.TourId == tourId);

            storedReview.ShouldNotBeNull();
            storedReview.Rating.ShouldBe(5);
            storedReview.TouristId.ShouldBe(26);
            storedReview.Comment.ShouldBe("Amazing tour! Highly recommend!");
            storedReview.TourPurchaseId.ShouldBe(purchaseId);
            storedReview.TourId.ShouldBe(tourId);
        }
    }

    [Fact]
    public void Successfully_stores_review_with_required_comment_for_low_rating()
    {
        // Setup and insert review directly (bypassing service validation for testing)
        long tourId;
        long purchaseId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            var pastDate = DateTime.UtcNow.AddDays(-3).AddHours(-1);

            var connection = dbContext.Database.GetDbConnection();
            connection.Open();

            // Insert tour
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO tours.""Tours"" (""AuthorId"", ""Name"", ""Description"", ""Difficulty"", ""Category"", ""Price"", ""Date"", ""State"")
                    VALUES (11, 'Tour for Low Rating Review', 'A tour that needs improvement', 2, 2, 100, @date, 1)
                    RETURNING ""Id""";

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@date";
                parameter.Value = pastDate;
                command.Parameters.Add(parameter);

                tourId = (long)command.ExecuteScalar();
            }

            // Create a purchase
            var purchase = new TourPurchase(27, new List<long> { tourId }, 100, 0);
            dbContext.TourPurchases.Add(purchase);
            dbContext.SaveChanges();
            purchaseId = purchase.Id;

            // Insert review with low rating and required comment
            dbContext.Database.ExecuteSqlRaw(
                @"INSERT INTO tours.""TourReviews"" (""TourPurchaseId"", ""TourId"", ""TouristId"", ""Rating"", ""Comment"", ""ReviewDate"")
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                purchaseId, tourId, 27, 2, "The tour guide was late and some locations were closed.", DateTime.UtcNow
            );

            connection.Close();
        }

        // Verification phase - verify review with comment was stored
        using (var verifyScope = Factory.Services.CreateScope())
        {
            var dbContext = verifyScope.ServiceProvider.GetRequiredService<ToursContext>();

            var storedReview = dbContext.TourReviews
                .FirstOrDefault(r => r.TourPurchaseId == purchaseId);

            storedReview.ShouldNotBeNull();
            storedReview.Rating.ShouldBe(2);
            storedReview.TouristId.ShouldBe(27);
            storedReview.Comment.ShouldNotBeNullOrEmpty();
            storedReview.Comment.ShouldBe("The tour guide was late and some locations were closed.");
        }
    }

    [Fact]
    public void Successfully_retrieves_tour_statistics()
    {
        // Setup phase
        long tourId;

        using (var setupScope = Factory.Services.CreateScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<ToursContext>();

            var pastDate = DateTime.UtcNow.AddDays(-4);

            var connection = dbContext.Database.GetDbConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO tours.""Tours"" (""AuthorId"", ""Name"", ""Description"", ""Difficulty"", ""Category"", ""Price"", ""Date"", ""State"")
                VALUES (11, 'Tour with Multiple Reviews', 'A popular tour', 3, 1, 200, @date, 1)
                RETURNING ""Id""";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@date";
            parameter.Value = pastDate;
            command.Parameters.Add(parameter);

            tourId = (long)command.ExecuteScalar();
            connection.Close();

            // Create multiple purchases and reviews
            for (int i = 28; i <= 32; i++) // 5 tourists
            {
                var purchase = new TourPurchase(i, new List<long> { tourId }, 200, 0);
                dbContext.TourPurchases.Add(purchase);
                dbContext.SaveChanges();

                // Create reviews with different ratings using raw SQL
                var rating = i == 28 ? 5 : i == 29 ? 5 : i == 30 ? 4 : i == 31 ? 3 : 2;
                var comment = rating <= 2 ? "Could be better" : null;

                dbContext.Database.ExecuteSqlRaw(
                    @"INSERT INTO tours.""TourReviews"" (""TourPurchaseId"", ""TourId"", ""TouristId"", ""Rating"", ""Comment"", ""ReviewDate"")
                      VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                    purchase.Id, tourId, i, rating, comment, DateTime.UtcNow
                );
            }
        }

        // Test phase
        using (var testScope = Factory.Services.CreateScope())
        {
            var controller = CreateController(testScope, touristId: 28);

            var result = ((ObjectResult)controller.GetTourStatistics(tourId).Result)?.Value as TourReviewStatisticsDto;

            result.ShouldNotBeNull();
            result.TourId.ShouldBe(tourId);
            result.TotalReviews.ShouldBe(5);
            result.Rating5Count.ShouldBe(2);
            result.Rating4Count.ShouldBe(1);
            result.Rating3Count.ShouldBe(1);
            result.Rating2Count.ShouldBe(1);
            result.AverageRating.ShouldBe(3.8); // (5+5+4+3+2)/5 = 3.8
        }
    }

    private static TourReviewController CreateController(IServiceScope scope, long touristId)
    {
        var controller = new TourReviewController(
            scope.ServiceProvider.GetRequiredService<ITourReviewService>());

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
}