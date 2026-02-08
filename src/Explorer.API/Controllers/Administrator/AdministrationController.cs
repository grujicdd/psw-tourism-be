using Explorer.Stakeholders.API.Dtos;
using Explorer.Stakeholders.API.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Administrator;

[Authorize(Policy = "administratorPolicy")]
[Route("api/administration")]
public class AdministrationController : BaseApiController
{
    private readonly IAdministrationService _administrationService;

    public AdministrationController(IAdministrationService administrationService)
    {
        _administrationService = administrationService;
    }

    [HttpGet("blocked-users")]
    public ActionResult<List<BlockedUserDto>> GetBlockedUsers()
    {
        var result = _administrationService.GetBlockedUsers();
        return CreateResponse(result);
    }

    [HttpPut("unblock-user/{userId}")]
    public ActionResult<BlockedUserDto> UnblockUser(long userId)
    {
        var result = _administrationService.UnblockUser(userId);
        return CreateResponse(result);
    }
}