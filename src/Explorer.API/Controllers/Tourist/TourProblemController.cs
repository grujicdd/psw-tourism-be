using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Explorer.API.Controllers.Tourist;

[Authorize(Policy = "touristPolicy")]
[Route("api/tourist/tour-problems")]
public class TourProblemController : BaseApiController
{
    private readonly ITourProblemService _tourProblemService;

    public TourProblemController(ITourProblemService tourProblemService)
    {
        _tourProblemService = tourProblemService;
    }

    [HttpPost]
    public ActionResult<TourProblemDto> ReportProblem([FromBody] CreateTourProblemDto problemDto)
    {
        var touristId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "0");
        var result = _tourProblemService.ReportProblem(touristId, problemDto);
        return CreateResponse(result);
    }

    [HttpGet]
    public ActionResult<PagedResult<TourProblemDto>> GetMyProblems([FromQuery] int page = 0, [FromQuery] int pageSize = 10)
    {
        var touristId = long.Parse(User.Claims.FirstOrDefault(c => c.Type == "id")?.Value ?? "0");
        var result = _tourProblemService.GetTouristProblems(touristId, page, pageSize);
        return CreateResponse(result);
    }

    [HttpGet("{id}")]
    public ActionResult<TourProblemDto> GetProblemById(long id)
    {
        var result = _tourProblemService.GetProblemById(id);
        return CreateResponse(result);
    }
}
