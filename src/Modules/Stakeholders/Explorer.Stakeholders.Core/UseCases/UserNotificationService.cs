// File: src/Modules/Stakeholders/Explorer.Stakeholders.Core/UseCases/UserNotificationService.cs
// CREATE NEW FILE

using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Public.Internal;
using Explorer.Stakeholders.Core.Domain;

namespace Explorer.Stakeholders.Core.UseCases;

public class UserNotificationService : IUserNotificationService
{
    private readonly ICrudRepository<User> _userRepository;
    private readonly ICrudRepository<Person> _personRepository;
    private readonly ICrudRepository<UserInterest> _userInterestRepository;

    public UserNotificationService(
        ICrudRepository<User> userRepository,
        ICrudRepository<Person> personRepository,
        ICrudRepository<UserInterest> userInterestRepository)
    {
        _userRepository = userRepository;
        _personRepository = personRepository;
        _userInterestRepository = userInterestRepository;
    }

    public List<string> GetInterestedUserEmails(int category)
    {
        try
        {
            // 1. Get all UserInterests for this category
            var userIdsWithInterest = _userInterestRepository.GetPaged(0, 1000)
                .Results
                .Where(ui => ui.InterestId == category)
                .Select(ui => ui.UserId)
                .ToHashSet();

            if (!userIdsWithInterest.Any())
            {
                return new List<string>();
            }

            // 2. Get Users with ReceiveRecommendations = true
            var eligibleUserIds = _userRepository.GetPaged(0, 1000)
                .Results
                .Where(u => userIdsWithInterest.Contains(u.Id) && u.ReceiveRecommendations)
                .Select(u => u.Id)
                .ToHashSet();

            if (!eligibleUserIds.Any())
            {
                return new List<string>();
            }

            // 3. Get Persons for these users and return their emails
            var emails = _personRepository.GetPaged(0, 1000)
                .Results
                .Where(p => eligibleUserIds.Contains(p.UserId))
                .Select(p => p.Email)
                .ToList();

            return emails;
        }
        catch (Exception ex)
        {
            // Log error and return empty list to avoid breaking tour creation
            Console.WriteLine($"Error getting interested users: {ex.Message}");
            return new List<string>();
        }
    }
}
