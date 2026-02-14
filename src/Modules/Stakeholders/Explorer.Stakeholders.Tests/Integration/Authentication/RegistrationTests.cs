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
public class RegistrationTests : BaseStakeholdersIntegrationTest
{
    public RegistrationTests(StakeholdersTestFactory factory) : base(factory) { }

    [Fact]
    public void Successfully_registers_tourist()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StakeholdersContext>();
        var controller = CreateController(scope);
        var account = new AccountRegistrationDto
        {
            Username = "turistaA@gmail.com",
            Email = "turistaA@gmail.com",
            Password = "turistaA",
            Name = "Žika",
            Surname = "Žikić",
            InterestsIds = new int[] { }
        };

        // Act
        var result = controller.RegisterTourist(account).Result;
        var authenticationResponse = ((ObjectResult)result).Value as AuthenticationTokensDto;

        // Assert - Response
        authenticationResponse.ShouldNotBeNull();
        authenticationResponse.Id.ShouldNotBe(0);
        authenticationResponse.AccessToken.ShouldNotBeNullOrWhiteSpace();

        var decodedAccessToken = new JwtSecurityTokenHandler().ReadJwtToken(authenticationResponse.AccessToken);
        var personId = decodedAccessToken.Claims.FirstOrDefault(c => c.Type == "personId");
        personId.ShouldNotBeNull();
        personId.Value.ShouldNotBe("0");

        // Assert - Database
        dbContext.ChangeTracker.Clear();
        var storedAccount = dbContext.Users.FirstOrDefault(u => u.Username == account.Email);
        storedAccount.ShouldNotBeNull();
        storedAccount.Role.ShouldBe(UserRole.Tourist);
        storedAccount.IsActive.ShouldBeTrue();

        var storedPerson = dbContext.People.FirstOrDefault(i => i.Email == account.Email);
        storedPerson.ShouldNotBeNull();
        storedPerson.UserId.ShouldBe(storedAccount.Id);
        storedPerson.Name.ShouldBe(account.Name);
        storedPerson.Surname.ShouldBe(account.Surname);
    }

    [Fact]
    public void Successfully_registers_tourist_with_interests()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StakeholdersContext>();
        var controller = CreateController(scope);

        // Assuming interests with IDs 1 and 2 exist in the database
        var account = new AccountRegistrationDto
        {
            Username = "turistaB@gmail.com",
            Email = "turistaB@gmail.com",
            Password = "turistaB123",
            Name = "Marko",
            Surname = "Marković",
            InterestsIds = new int[] { 1, 2 }
        };

        // Act
        var result = controller.RegisterTourist(account).Result;
        var authenticationResponse = ((ObjectResult)result).Value as AuthenticationTokensDto;

        // Assert - Response
        authenticationResponse.ShouldNotBeNull();
        authenticationResponse.Id.ShouldNotBe(0);

        // Assert - Database
        dbContext.ChangeTracker.Clear();
        var storedAccount = dbContext.Users.FirstOrDefault(u => u.Username == account.Email);
        storedAccount.ShouldNotBeNull();

        var storedPerson = dbContext.People.FirstOrDefault(i => i.Email == account.Email);
        storedPerson.ShouldNotBeNull();

        // Verify interests are saved
        var userInterests = dbContext.UserInterests.Where(ui => ui.UserId == storedAccount.Id).ToList();
        userInterests.Count.ShouldBe(2);
        userInterests.Any(ui => ui.InterestId == 1).ShouldBeTrue();
        userInterests.Any(ui => ui.InterestId == 2).ShouldBeTrue();
    }

    [Fact]
    public void Fails_to_register_tourist_with_duplicate_username()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);

        var firstAccount = new AccountRegistrationDto
        {
            Username = "duplicate@gmail.com",
            Email = "duplicate@gmail.com",
            Password = "password123",
            Name = "First",
            Surname = "User",
            InterestsIds = new int[] { }
        };

        var duplicateAccount = new AccountRegistrationDto
        {
            Username = "duplicate@gmail.com",
            Email = "different@gmail.com",
            Password = "password456",
            Name = "Second",
            Surname = "User",
            InterestsIds = new int[] { }
        };

        // Act
        var firstResult = controller.RegisterTourist(firstAccount).Result; // First registration succeeds
        var secondResult = (ObjectResult)controller.RegisterTourist(duplicateAccount).Result;

        // Assert
        secondResult.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_register_tourist_with_missing_required_fields()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);

        var accountWithoutName = new AccountRegistrationDto
        {
            Username = "missingname@gmail.com",
            Email = "missingname@gmail.com",
            Password = "password123",
            Name = "", // Missing required field
            Surname = "TestSurname",
            InterestsIds = new int[] { }
        };

        // Act
        var result = (ObjectResult)controller.RegisterTourist(accountWithoutName).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_register_tourist_with_invalid_email_format()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);

        var accountWithInvalidEmail = new AccountRegistrationDto
        {
            Username = "invalidemail",
            Email = "invalidemail", // Invalid email format
            Password = "password123",
            Name = "Test",
            Surname = "User",
            InterestsIds = new int[] { }
        };

        // Act
        var result = (ObjectResult)controller.RegisterTourist(accountWithInvalidEmail).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    [Fact]
    public void Fails_to_register_tourist_with_weak_password()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var controller = CreateController(scope);

        var accountWithWeakPassword = new AccountRegistrationDto
        {
            Username = "weakpass@gmail.com",
            Email = "weakpass@gmail.com",
            Password = "12", // Too short password
            Name = "Test",
            Surname = "User",
            InterestsIds = new int[] { }
        };

        // Act
        var result = (ObjectResult)controller.RegisterTourist(accountWithWeakPassword).Result;

        // Assert
        result.StatusCode.ShouldBe(400);
    }

    private static AuthenticationController CreateController(IServiceScope scope)
    {
        return new AuthenticationController(scope.ServiceProvider.GetRequiredService<IAuthenticationService>());
    }
}