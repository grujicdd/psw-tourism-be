using Explorer.Stakeholders.API.Dtos;
using FluentResults;

namespace Explorer.Stakeholders.API.Public;

public interface ITouristProfileService
{
    Result<TouristProfileDto> GetProfile(long userId);
    Result<TouristProfileDto> UpdateProfile(long userId, UpdateTouristProfileDto dto);
}
