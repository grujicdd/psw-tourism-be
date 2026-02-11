using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Administrator;

[Authorize(Policy = "administratorPolicy")]
[Route("api/administrator/tour-problems")]
public class AdminTourProblemController : BaseApiController
{
    private readonly ITourProblemService _tourProblemService;

    public AdminTourProblemController(ITourProblemService tourProblemService)
    {
        _tourProblemService = tourProblemService;
    }

    [HttpGet("under-review")]
    public ActionResult<PagedResult<TourProblemDto>> GetProblemsUnderReview([FromQuery] int page = 0, [FromQuery] int pageSize = 10)
    {
        var result = _tourProblemService.GetProblemsUnderReview(page, pageSize);
        return CreateResponse(result);
    }

    [HttpPut("{id}/return-to-guide")]
    public ActionResult<TourProblemDto> ReturnToGuide(long id)
    {
        var result = _tourProblemService.ReturnProblemToGuide(id);
        return CreateResponse(result);
    }

    [HttpPut("{id}/reject")]
    public ActionResult<TourProblemDto> RejectProblem(long id)
    {
        var result = _tourProblemService.RejectProblem(id);
        return CreateResponse(result);
    }
}
