using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Explorer.API.Controllers;
using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Explorer.Stakeholders.Infrastructure.Database;
using Explorer.Stakeholders.Core.Domain;

namespace Explorer.Stakeholders.Tests.Integration.Authentication;

[Collection("Sequential")]
public class LoginTests : BaseStakeholdersIntegrationTest
{
    public LoginTests(StakeholdersTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_logs_in_with_valid_credentials()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);
        var credentials = new CredentialsDto
        {
            Username = "turista1@gmail.com",
            Password = "turista1"
        };

        // Act
        var result = controller.Login(credentials).Result;
        var authenticationResponse = ((ObjectResult)result).Value as AuthenticationTokensDto;

        // Assert
        authenticationResponse.ShouldNotBeNull();
        authenticationResponse.Id.ShouldNotBe(0);
        authenticationResponse.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var decodedAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(authenticationResponse.AccessToken);
        var username = decodedAccessToken.Claims.FirstOrDefault(c => c.Type == "username");
        username.ShouldNotBeNull();
        username.Value.ShouldBe("turista1@gmail.com");

        var personId = decodedAccessToken.Claims.FirstOrDefault(c => c.Type == "personId");
        personId.ShouldNotBeNull();
        personId.Value.ShouldBe("-21"); // From test data
    }

    [Fact]
    public void Fails_to_login_with_wrong_password()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);
        var credentials = new CredentialsDto
        {
            Username = "turista1@gmail.com",
            Password = "wrongpassword"
        };

        // Act
        var result = (ObjectResult)controller.Login(credentials).Result;

        // Assert
        result.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void Fails_to_login_with_nonexistent_username()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);
        var credentials = new CredentialsDto
        {
            Username = "nonexistent@gmail.com",
            Password = "somepassword"
        };

        // Act
        var result = (ObjectResult)controller.Login(credentials).Result;

        // Assert
        result.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void Blocks_user_after_5_failed_login_attempts()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);
        var dbContext = scope.ServiceProvider.GetRequiredService<StakeholdersContext>();

        var credentials = new CredentialsDto
        {
            Username = "turista2@gmail.com",
            Password = "wrongpassword"
        };

        // Act - Attempt login 5 times with wrong password
        for (int i = 0; i < 5; i++)
        {
            var _ = controller.Login(credentials).Result;
        }

        // Assert - Verify user is blocked in database
        dbContext.ChangeTracker.Clear();
        var user = dbContext.Users.FirstOrDefault(u => u.Username == "turista2@gmail.com");
        user.ShouldNotBeNull();
        user.IsActive.ShouldBeFalse();
        user.BlockCount.ShouldBe(1);
        user.FailedLoginAttempts.ShouldBe(0); // Reset after blocking

        // Act - Try to login with correct password while blocked
        credentials.Password = "turista2";
        var result = (ObjectResult)controller.Login(credentials).Result;

        // Assert - Login should fail with 403 Forbidden
        result.StatusCode.ShouldBe(403);
    }

    [Fact]
    public void Resets_failed_attempts_after_successful_login()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);
        var dbContext = scope.ServiceProvider.GetRequiredService<StakeholdersContext>();

        var wrongCredentials = new CredentialsDto
        {
            Username = "turista3@gmail.com",
            Password = "wrongpassword"
        };

        var correctCredentials = new CredentialsDto
        {
            Username = "turista3@gmail.com",
            Password = "turista3"
        };

        // Act - Fail login 3 times
        for (int i = 0; i < 3; i++)
        {
            var _ = controller.Login(wrongCredentials).Result;
        }

        // Verify attempts were incremented
        dbContext.ChangeTracker.Clear();
        var userAfterFailures = dbContext.Users.FirstOrDefault(u => u.Username == "turista3@gmail.com");
        userAfterFailures.FailedLoginAttempts.ShouldBe(3);

        // Act - Login successfully
        var result = controller.Login(correctCredentials).Result;
        var authenticationResponse = ((ObjectResult)result).Value as AuthenticationTokensDto;

        // Assert - Login succeeds
        authenticationResponse.ShouldNotBeNull();

        // Assert - Failed attempts reset to 0
        dbContext.ChangeTracker.Clear();
        var userAfterSuccess = dbContext.Users.FirstOrDefault(u => u.Username == "turista3@gmail.com");
        userAfterSuccess.FailedLoginAttempts.ShouldBe(0);
        userAfterSuccess.IsActive.ShouldBeTrue();
    }

    private static AuthenticationController CreateController(IServiceScope scope)
    {
        return new AuthenticationController(scope.ServiceProvider.GetRequiredService<IAuthenticationService>());
    }
}