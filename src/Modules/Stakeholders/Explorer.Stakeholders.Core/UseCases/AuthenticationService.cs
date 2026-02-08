using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Stakeholders.Core.Domain.RepositoryInterfaces;
using FluentResults;

namespace Explorer.Stakeholders.Core.UseCases;

public class AuthenticationService : IAuthenticationService
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IUserRepository _userRepository;
    private readonly ICrudRepository<User> _crudUserRepository;
    private readonly ICrudRepository<Person> _personRepository;
    private readonly ICrudRepository<UserInterest> _userInterestRepository;

    public AuthenticationService(IUserRepository userRepository, ICrudRepository<User> crudUserRepository,
        ICrudRepository<Person> personRepository, ITokenGenerator tokenGenerator,
        ICrudRepository<UserInterest> userInterestRepository)
    {
        _tokenGenerator = tokenGenerator;
        _userRepository = userRepository;
        _crudUserRepository = crudUserRepository;
        _personRepository = personRepository;
        _userInterestRepository = userInterestRepository;
    }

    public Result<AuthenticationTokensDto> Login(CredentialsDto credentials)
    {
        var user = _userRepository.GetActiveByName(credentials.Username);

        // If user doesn't exist or is blocked
        if (user == null)
        {
            // Try to find the user even if blocked to increment attempts
            var blockedOrNonExistentUser = _crudUserRepository.GetPaged(0, 1000)
                .Results
                .FirstOrDefault(u => u.Username == credentials.Username);

            if (blockedOrNonExistentUser != null && !blockedOrNonExistentUser.IsActive)
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("Account is temporarily blocked due to multiple failed login attempts");
            }

            // User doesn't exist or wrong username - increment attempts for any matching username
            if (blockedOrNonExistentUser != null)
            {
                blockedOrNonExistentUser.IncrementFailedLoginAttempts();
                _crudUserRepository.Update(blockedOrNonExistentUser);
            }

            return Result.Fail(FailureCode.NotFound);
        }

        // Verify password
        if (user.Password != credentials.Password)
        {
            user.IncrementFailedLoginAttempts();
            _crudUserRepository.Update(user);

            if (!user.IsActive)
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("Account has been blocked due to multiple failed login attempts");
            }

            return Result.Fail(FailureCode.NotFound);
        }

        // Successful login - reset failed attempts
        user.ResetFailedLoginAttempts();
        _crudUserRepository.Update(user);

        long personId;
        try
        {
            personId = _userRepository.GetPersonId(user.Id);
        }
        catch (KeyNotFoundException)
        {
            personId = 0;
        }

        return _tokenGenerator.GenerateAccessToken(user, personId);
    }

    public Result<AuthenticationTokensDto> RegisterTourist(AccountRegistrationDto account)
    {
        if (_userRepository.Exists(account.Username)) return Result.Fail(FailureCode.NonUniqueUsername);

        try
        {
            var user = _userRepository.Create(new User(account.Username, account.Password, UserRole.Tourist, true));
            var person = _personRepository.Create(new Person(user.Id, account.Name, account.Surname, account.Email));

            // Create UserInterest records if interests are provided
            if (account.InterestsIds != null && account.InterestsIds.Any())
            {
                foreach (int interestId in account.InterestsIds)
                {
                    _userInterestRepository.Create(new UserInterest(person.UserId, interestId));
                }
            }

            return _tokenGenerator.GenerateAccessToken(user, person.Id);
        }
        catch (ArgumentException e)
        {
            _userRepository.Delete(account.Username);
            return Result.Fail(FailureCode.InvalidArgument).WithError(e.Message);
        }
    }
}