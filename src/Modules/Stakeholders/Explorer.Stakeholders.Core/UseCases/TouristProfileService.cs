using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Stakeholders.Core.Domain.RepositoryInterfaces;
using FluentResults;

namespace Explorer.Stakeholders.Core.UseCases;

public class TouristProfileService : ITouristProfileService
{
    private readonly ICrudRepository<User> _userRepository;
    private readonly ICrudRepository<Person> _personRepository;
    private readonly IUserInterestRepository _userInterestRepository;
    private readonly IMapper _mapper;

    public TouristProfileService(
        ICrudRepository<User> userRepository,
        ICrudRepository<Person> personRepository,
        IUserInterestRepository userInterestRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _personRepository = personRepository;
        _userInterestRepository = userInterestRepository;
        _mapper = mapper;
    }

    public Result<TouristProfileDto> GetProfile(long userId)
    {
        try
        {
            var user = _userRepository.Get(userId);
            if (user == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("User not found");
            }

            var person = _personRepository.GetPaged(0, 1)
                .Results
                .FirstOrDefault(p => p.UserId == userId);

            if (person == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Person not found");
            }

            var userInterests = _userInterestRepository.GetByUserId(userId)
                .Select(ui => (int)ui.InterestId)
                .ToList();

            var profileDto = new TouristProfileDto
            {
                Id = person.Id,
                Name = person.Name,
                Surname = person.Surname,
                Email = person.Email,
                InterestIds = userInterests,
                ReceiveRecommendations = user.ReceiveRecommendations
            };

            return Result.Ok(profileDto);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
    }

    public Result<TouristProfileDto> UpdateProfile(long userId, UpdateTouristProfileDto dto)
    {
        try
        {
            // 1. Update User.ReceiveRecommendations
            var user = _userRepository.Get(userId);
            if (user == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("User not found");
            }

            user.UpdateRecommendationPreference(dto.ReceiveRecommendations);
            _userRepository.Update(user);

            // 2. Delete all existing UserInterests for this user
            _userInterestRepository.DeleteAllByUserId(userId);

            // 3. Create new UserInterests
            foreach (var interestId in dto.InterestIds)
            {
                _userInterestRepository.CreateUserInterest(userId, interestId);
            }

            // 4. Return updated profile
            return GetProfile(userId);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
    }
}