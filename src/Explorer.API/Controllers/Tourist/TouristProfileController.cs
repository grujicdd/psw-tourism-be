using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist;

[Authorize(Policy = "touristPolicy")]
[Route("api/tourist/profile")]
public class TouristProfileController : BaseApiController
{
    private readonly ITouristProfileService _profileService;

    public TouristProfileController(ITouristProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public ActionResult<TouristProfileDto> GetProfile()
    {
        var userId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
        var result = _profileService.GetProfile(userId);
        return CreateResponse(result);
    }

    [HttpPut]
    public ActionResult<TouristProfileDto> UpdateProfile([FromBody] UpdateTouristProfileDto dto)
    {
        var userId = long.Parse(User.Claims.First(c => c.Type == "id").Value);
        var result = _profileService.UpdateProfile(userId, dto);
        return CreateResponse(result);
    }
}
