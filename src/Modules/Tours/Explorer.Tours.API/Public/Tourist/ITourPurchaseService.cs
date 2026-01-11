using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist
{
    public interface ITourPurchaseService
    {
        Result<TourPurchaseDto> ProcessPurchase(long touristId, decimal bonusPointsToUse);
        Result<PagedResult<TourPurchaseDto>> GetPurchaseHistory(long touristId, int page, int pageSize);
        Result<TourPurchaseDto> GetPurchase(long purchaseId);
    }
}
