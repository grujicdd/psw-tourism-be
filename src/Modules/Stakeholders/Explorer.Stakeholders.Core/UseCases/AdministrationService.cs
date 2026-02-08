using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Explorer.Stakeholders.Core.Domain;
using FluentResults;

namespace Explorer.Stakeholders.Core.UseCases;

public class AdministrationService : IAdministrationService
{
    private readonly ICrudRepository<User> _userRepository;
    private readonly ICrudRepository<Person> _personRepository;

    public AdministrationService(ICrudRepository<User> userRepository, ICrudRepository<Person> personRepository)
    {
        _userRepository = userRepository;
        _personRepository = personRepository;
    }

    public Result<List<BlockedUserDto>> GetBlockedUsers()
    {
        try
        {
            // Get all blocked users (IsActive = false and BlockCount > 0)
            var blockedUsers = _userRepository.GetPaged(0, 1000)
                .Results
                .Where(u => !u.IsActive && u.BlockCount > 0)
                .ToList();

            if (!blockedUsers.Any())
            {
                return Result.Ok(new List<BlockedUserDto>());
            }

            // Get person details for each blocked user
            var blockedUserDtos = new List<BlockedUserDto>();

            foreach (var user in blockedUsers)
            {
                var person = _personRepository.GetPaged(0, 1000)
                    .Results
                    .FirstOrDefault(p => p.UserId == user.Id);

                if (person != null)
                {
                    blockedUserDtos.Add(new BlockedUserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Name = person.Name,
                        Surname = person.Surname,
                        Role = user.GetPrimaryRoleName(),
                        BlockCount = user.BlockCount,
                        CanBeUnblocked = user.CanBeUnblocked()
                    });
                }
            }

            return Result.Ok(blockedUserDtos);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<BlockedUserDto> UnblockUser(long userId)
    {
        try
        {
            var user = _userRepository.Get(userId);

            if (user == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("User not found");
            }

            if (user.IsActive)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError("User is not blocked");
            }

            if (!user.CanBeUnblocked())
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("User has been blocked 3 times and cannot be unblocked");
            }

            user.Unblock();
            _userRepository.Update(user);

            var person = _personRepository.GetPaged(0, 1000)
                .Results
                .FirstOrDefault(p => p.UserId == user.Id);

            var dto = new BlockedUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = person?.Name ?? "",
                Surname = person?.Surname ?? "",
                Role = user.GetPrimaryRoleName(),
                BlockCount = user.BlockCount,
                CanBeUnblocked = user.CanBeUnblocked()
            };

            return Result.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(FailureCode.Forbidden).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }
}
