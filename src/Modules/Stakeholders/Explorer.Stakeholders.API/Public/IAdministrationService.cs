using Explorer.Stakeholders.API.Dtos;
using FluentResults;

namespace Explorer.Stakeholders.API.Public;

public interface IAdministrationService
{
    Result<List<BlockedUserDto>> GetBlockedUsers();
    Result<BlockedUserDto> UnblockUser(long userId);
}