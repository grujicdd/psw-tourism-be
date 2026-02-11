using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public
{
    public interface ITourReplacementService
    {
        // Guide requests replacement for their tour
        Result<TourReplacementDto> RequestReplacement(long guideId, long tourId);

        // Guide cancels their replacement request
        Result CancelReplacementRequest(long guideId, long replacementId);

        // Get all tours available for replacement (PENDING status, excluding own tours and date conflicts)
        Result<PagedResult<AvailableTourReplacementDto>> GetAvailableReplacements(long guideId, int page, int pageSize);

        // Guide applies to replace a tour
        Result<TourReplacementDto> AcceptReplacement(long guideId, long replacementId);

        // Get replacement requests by guide (for original guide to see their requests)
        Result<PagedResult<TourReplacementDto>> GetMyReplacementRequests(long guideId, int page, int pageSize);

        // Get replacement with tour details (for displaying in UI)
        Result<AvailableTourReplacementDto> GetReplacementWithTourDetails(long replacementId);
    }
}