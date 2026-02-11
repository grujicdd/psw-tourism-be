using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist;

public class TourProblemService : ITourProblemService
{
    private readonly ICrudRepository<TourProblem> _problemRepository;
    private readonly ICrudRepository<Tour> _tourRepository;
    private readonly ICrudRepository<TourPurchase> _purchaseRepository;
    private readonly IMapper _mapper;

    public TourProblemService(
        ICrudRepository<TourProblem> problemRepository,
        ICrudRepository<Tour> tourRepository,
        ICrudRepository<TourPurchase> purchaseRepository,
        IMapper mapper)
    {
        _problemRepository = problemRepository;
        _tourRepository = tourRepository;
        _purchaseRepository = purchaseRepository;
        _mapper = mapper;
    }

    // TOURIST ACTIONS
    public Result<TourProblemDto> ReportProblem(long touristId, CreateTourProblemDto problemDto)
    {
        try
        {
            // Verify tour exists
            var tour = _tourRepository.Get(problemDto.TourId);
            if (tour == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
            }

            // Verify tourist has purchased this tour
            var hasPurchased = _purchaseRepository.GetPaged(0, 1000)
                .Results
                .Any(p => p.TouristId == touristId && p.ContainsTour(problemDto.TourId));

            if (!hasPurchased)
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("You can only report problems for tours you have purchased");
            }

            // Create problem
            var problem = new TourProblem(
                problemDto.TourId,
                touristId,
                problemDto.Title,
                problemDto.Description
            );

            var createdProblem = _problemRepository.Create(problem);
            var problemDto_result = MapToProblemDto(createdProblem);

            return Result.Ok(problemDto_result);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<PagedResult<TourProblemDto>> GetTouristProblems(long touristId, int page, int pageSize)
    {
        try
        {
            var allProblems = _problemRepository.GetPaged(0, 10000) // Get all, then filter
                .Results
                .Where(p => p.TouristId == touristId)
                .OrderByDescending(p => p.ReportedAt)
                .ToList();

            var totalCount = allProblems.Count;
            var pagedProblems = allProblems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = pagedProblems.Select(MapToProblemDto).ToList();

            return Result.Ok(new PagedResult<TourProblemDto>(dtos, totalCount));
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<TourProblemDto> GetProblemById(long problemId)
    {
        try
        {
            var problem = _problemRepository.Get(problemId);
            if (problem == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Problem not found");
            }

            var dto = MapToProblemDto(problem);
            return Result.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    // GUIDE ACTIONS
    public Result<PagedResult<TourProblemDto>> GetProblemsByGuide(long guideId, int page, int pageSize)
    {
        try
        {
            // Get all tours by this guide
            var guideTourIds = _tourRepository.GetPaged(0, 10000)
                .Results
                .Where(t => t.AuthorId == guideId)
                .Select(t => t.Id)
                .ToList();

            // Get problems for those tours (Pending or UnderReview)
            var allProblems = _problemRepository.GetPaged(0, 10000)
                .Results
                .Where(p => guideTourIds.Contains(p.TourId))
                .OrderByDescending(p => p.ReportedAt)
                .ToList();

            var totalCount = allProblems.Count;
            var pagedProblems = allProblems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = pagedProblems.Select(MapToProblemDto).ToList();

            return Result.Ok(new PagedResult<TourProblemDto>(dtos, totalCount));
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<TourProblemDto> MarkProblemAsResolved(long problemId, long guideId)
    {
        try
        {
            var problem = _problemRepository.Get(problemId);
            if (problem == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Problem not found");
            }

            // Verify guide owns the tour
            var tour = _tourRepository.Get(problem.TourId);
            if (tour == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
            }

            if (tour.AuthorId != guideId)
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("You can only resolve problems on your own tours");
            }

            problem.MarkAsResolved();
            _problemRepository.Update(problem);

            var dto = MapToProblemDto(problem);
            return Result.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<TourProblemDto> SendProblemToAdministrator(long problemId, long guideId)
    {
        try
        {
            var problem = _problemRepository.Get(problemId);
            if (problem == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Problem not found");
            }

            // Verify guide owns the tour
            var tour = _tourRepository.Get(problem.TourId);
            if (tour == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
            }

            if (tour.AuthorId != guideId)
            {
                return Result.Fail(FailureCode.Forbidden)
                    .WithError("You can only send problems to administrator for your own tours");
            }

            problem.SendToAdministrator();
            _problemRepository.Update(problem);

            var dto = MapToProblemDto(problem);
            return Result.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    // ADMINISTRATOR ACTIONS
    public Result<PagedResult<TourProblemDto>> GetProblemsUnderReview(int page, int pageSize)
    {
        try
        {
            var allProblems = _problemRepository.GetPaged(0, 10000)
                .Results
                .Where(p => p.IsUnderReview())
                .OrderByDescending(p => p.ReviewRequestedAt)
                .ToList();

            var totalCount = allProblems.Count;
            var pagedProblems = allProblems
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = pagedProblems.Select(MapToProblemDto).ToList();

            return Result.Ok(new PagedResult<TourProblemDto>(dtos, totalCount));
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<TourProblemDto> ReturnProblemToGuide(long problemId)
    {
        try
        {
            var problem = _problemRepository.Get(problemId);
            if (problem == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Problem not found");
            }

            problem.ReturnToGuide();
            _problemRepository.Update(problem);

            var dto = MapToProblemDto(problem);
            return Result.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    public Result<TourProblemDto> RejectProblem(long problemId)
    {
        try
        {
            var problem = _problemRepository.Get(problemId);
            if (problem == null)
            {
                return Result.Fail(FailureCode.NotFound).WithError("Problem not found");
            }

            problem.Reject();
            _problemRepository.Update(problem);

            var dto = MapToProblemDto(problem);
            return Result.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
        }
        catch (Exception ex)
        {
            return Result.Fail(FailureCode.Internal).WithError(ex.Message);
        }
    }

    // HELPER METHOD
    private TourProblemDto MapToProblemDto(TourProblem problem)
    {
        var tour = _tourRepository.Get(problem.TourId);

        return new TourProblemDto
        {
            Id = problem.Id,
            TourId = problem.TourId,
            TouristId = problem.TouristId,
            Title = problem.Title,
            Description = problem.Description,
            Status = (int)problem.Status,
            StatusName = problem.Status.ToString(),
            ReportedAt = problem.ReportedAt,
            ResolvedAt = problem.ResolvedAt,
            ReviewRequestedAt = problem.ReviewRequestedAt,
            RejectedAt = problem.RejectedAt,
            TourName = tour?.Name ?? "Unknown Tour",
            TouristName = "" // Will be populated by controller if needed
        };
    }
}