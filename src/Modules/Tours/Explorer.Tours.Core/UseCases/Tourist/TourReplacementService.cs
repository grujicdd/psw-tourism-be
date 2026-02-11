using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Guide
{
    public class TourReplacementService : CrudService<TourReplacementDto, TourReplacement>, ITourReplacementService
    {
        private readonly ICrudRepository<Tour> _tourRepository;

        public TourReplacementService(
            ICrudRepository<TourReplacement> repository,
            ICrudRepository<Tour> tourRepository,
            IMapper mapper) : base(repository, mapper)
        {
            _tourRepository = tourRepository;
        }

        public Result<TourReplacementDto> RequestReplacement(long guideId, long tourId)
        {
            try
            {
                // Get the tour
                var tour = _tourRepository.Get(tourId);
                if (tour == null)
                    return Result.Fail("Tour not found");

                // Verify the guide owns this tour
                if (tour.AuthorId != guideId)
                    return Result.Fail("You can only request replacement for your own tours");

                // Verify tour is in COMPLETE state (published)
                if (tour.State != TourState.COMPLETE)
                    return Result.Fail("Only published tours can request replacement");

                // Verify tour date is in the future
                if (tour.Date <= DateTime.UtcNow)
                    return Result.Fail("Cannot request replacement for past tours");

                // Check if there's already a pending replacement request for this tour
                var existingReplacement = CrudRepository.GetAll()
                    .FirstOrDefault(r => r.TourId == tourId && r.Status == Domain.TourReplacementStatus.PENDING);

                if (existingReplacement != null)
                    return Result.Fail("There is already a pending replacement request for this tour");

                // Create replacement request
                var replacement = new TourReplacement(tourId, guideId);
                var created = CrudRepository.Create(replacement);

                return MapToDto(created);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error requesting replacement: {ex.Message}");
            }
        }

        public Result CancelReplacementRequest(long guideId, long replacementId)
        {
            try
            {
                var replacement = CrudRepository.Get(replacementId);
                if (replacement == null)
                    return Result.Fail("Replacement request not found");

                // Verify the guide owns this replacement request
                if (replacement.OriginalGuideId != guideId)
                    return Result.Fail("You can only cancel your own replacement requests");

                // Verify status is PENDING
                if (replacement.Status != Domain.TourReplacementStatus.PENDING)
                    return Result.Fail($"Cannot cancel replacement request with status {replacement.Status}");

                replacement.CancelReplacement();
                CrudRepository.Update(replacement);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error cancelling replacement request: {ex.Message}");
            }
        }

        public Result<PagedResult<AvailableTourReplacementDto>> GetAvailableReplacements(long guideId, int page, int pageSize)
        {
            try
            {
                // Get all PENDING replacements where the guide is NOT the original guide
                var pendingReplacements = CrudRepository.GetAll()
                    .Where(r => r.Status == Domain.TourReplacementStatus.PENDING && r.OriginalGuideId != guideId)
                    .ToList();

                // Get all tours for these replacements
                var tourIds = pendingReplacements.Select(r => r.TourId).ToList();
                var tours = _tourRepository.GetAll()
                    .Where(t => tourIds.Contains(t.Id) && t.Date > DateTime.UtcNow)
                    .ToList();

                // Get all tour dates where this guide already has a tour
                var guideTourDates = _tourRepository.GetAll()
                    .Where(t => t.AuthorId == guideId && t.Date > DateTime.UtcNow)
                    .Select(t => t.Date.Date)
                    .ToHashSet();

                // Join replacements with tours and filter out date conflicts
                var availableReplacements = (from replacement in pendingReplacements
                                             join tour in tours on replacement.TourId equals tour.Id
                                             where !guideTourDates.Contains(tour.Date.Date)
                                             orderby tour.Date
                                             select new AvailableTourReplacementDto
                                             {
                                                 ReplacementId = replacement.Id,
                                                 TourId = tour.Id,
                                                 TourName = tour.Name,
                                                 TourDescription = tour.Description,
                                                 TourDate = tour.Date,
                                                 TourDifficulty = tour.Difficulty,
                                                 TourCategory = tour.Category,
                                                 TourPrice = tour.Price,
                                                 OriginalGuideId = replacement.OriginalGuideId,
                                                 RequestedAt = replacement.RequestedAt
                                             }).ToList();

                var totalCount = availableReplacements.Count;
                var pagedItems = availableReplacements
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Result.Ok(new PagedResult<AvailableTourReplacementDto>(pagedItems, totalCount));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error getting available replacements: {ex.Message}");
            }
        }

        public Result<TourReplacementDto> AcceptReplacement(long guideId, long replacementId)
        {
            try
            {
                var replacement = CrudRepository.Get(replacementId);
                if (replacement == null)
                    return Result.Fail("Replacement request not found");

                // Verify replacement is PENDING
                if (replacement.Status != Domain.TourReplacementStatus.PENDING)
                    return Result.Fail($"Cannot accept replacement with status {replacement.Status}");

                // Verify guide is not the original guide
                if (replacement.OriginalGuideId == guideId)
                    return Result.Fail("You cannot accept your own replacement request");

                // Get the tour
                var tour = _tourRepository.Get(replacement.TourId);
                if (tour == null)
                    return Result.Fail("Tour not found");

                // Verify tour is in the future
                if (tour.Date <= DateTime.UtcNow)
                    return Result.Fail("Cannot accept replacement for past tours");

                // Verify guide doesn't already have a tour on that date
                var hasConflict = _tourRepository.GetAll()
                    .Any(t => t.AuthorId == guideId
                           && t.Date.Date == tour.Date.Date
                           && t.Id != tour.Id);

                if (hasConflict)
                    return Result.Fail("You already have a tour scheduled on this date");

                // Accept the replacement
                replacement.AcceptReplacement(guideId);

                // Update tour author to the new guide
                tour.AuthorId = guideId;
                _tourRepository.Update(tour);

                // Update replacement
                var updated = CrudRepository.Update(replacement);

                return MapToDto(updated);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error accepting replacement: {ex.Message}");
            }
        }

        public Result<PagedResult<TourReplacementDto>> GetMyReplacementRequests(long guideId, int page, int pageSize)
        {
            try
            {
                var query = CrudRepository.GetAll()
                    .Where(r => r.OriginalGuideId == guideId)
                    .OrderByDescending(r => r.RequestedAt);

                var totalCount = query.Count();

                var replacements = query
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList();

                var dtos = replacements.Select(r => MapToDto(r)).ToList();

                return Result.Ok(new PagedResult<TourReplacementDto>(dtos, totalCount));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error getting replacement requests: {ex.Message}");
            }
        }

        public Result<AvailableTourReplacementDto> GetReplacementWithTourDetails(long replacementId)
        {
            try
            {
                var replacement = CrudRepository.Get(replacementId);
                if (replacement == null)
                    return Result.Fail("Replacement request not found");

                var tour = _tourRepository.Get(replacement.TourId);
                if (tour == null)
                    return Result.Fail("Tour not found");

                var dto = new AvailableTourReplacementDto
                {
                    ReplacementId = replacement.Id,
                    TourId = tour.Id,
                    TourName = tour.Name,
                    TourDescription = tour.Description,
                    TourDate = tour.Date,
                    TourDifficulty = tour.Difficulty,
                    TourCategory = tour.Category,
                    TourPrice = tour.Price,
                    OriginalGuideId = replacement.OriginalGuideId,
                    RequestedAt = replacement.RequestedAt
                };

                return Result.Ok(dto);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error getting replacement details: {ex.Message}");
            }
        }
    }
}