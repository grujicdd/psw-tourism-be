using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Author;

[Authorize(Policy = "authorPolicy")]
[Route("api/author/tour-problems")]
public class GuideTourProblemController : BaseApiController
{
    private readonly ITourProblemService _tourProblemService;

    public GuideTourProblemController(ITourProblemService tourProblemService)
    {
        _tourProblemService = tourProblemService;
    }

    [HttpGet]
    public ActionResult<PagedResult<TourProblemDto>> GetMyTourProblems([FromQuery] int page = 0, [FromQuery] int pageSize = 10)
    {
        var guideId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "0");
        var result = _tourProblemService.GetProblemsByGuide(guideId, page, pageSize);
        return CreateResponse(result);
    }

    [HttpPut("{id}/resolve")]
    public ActionResult<TourProblemDto> MarkAsResolved(long id)
    {
        var guideId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "0");
        var result = _tourProblemService.MarkProblemAsResolved(id, guideId);
        return CreateResponse(result);
    }

    [HttpPut("{id}/send-to-admin")]
    public ActionResult<TourProblemDto> SendToAdministrator(long id)
    {
        var guideId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "0");
        var result = _tourProblemService.SendProblemToAdministrator(id, guideId);
        return CreateResponse(result);
    }
}
