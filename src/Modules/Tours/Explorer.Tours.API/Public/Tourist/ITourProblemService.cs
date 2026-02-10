using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist;

public interface ITourProblemService
{
    // Tourist actions
    Result<TourProblemDto> ReportProblem(long touristId, CreateTourProblemDto problemDto);
    Result<PagedResult<TourProblemDto>> GetTouristProblems(long touristId, int page, int pageSize);
    Result<TourProblemDto> GetProblemById(long problemId);

    // Guide actions
    Result<PagedResult<TourProblemDto>> GetProblemsByGuide(long guideId, int page, int pageSize);
    Result<TourProblemDto> MarkProblemAsResolved(long problemId, long guideId);
    Result<TourProblemDto> SendProblemToAdministrator(long problemId, long guideId);

    // Administrator actions
    Result<PagedResult<TourProblemDto>> GetProblemsUnderReview(int page, int pageSize);
    Result<TourProblemDto> ReturnProblemToGuide(long problemId);
    Result<TourProblemDto> RejectProblem(long problemId);
}
