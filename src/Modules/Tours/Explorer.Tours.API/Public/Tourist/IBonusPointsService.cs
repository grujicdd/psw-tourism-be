using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist
{
    public interface IBonusPointsService
    {
        Result<BonusPointsDto> GetBonusPoints(long touristId);
        Result<BonusPointsDto> AddBonusPoints(long touristId, decimal amount, string reason, long? relatedTourId = null);
        Result<BonusPointsDto> UseBonusPoints(long touristId, decimal amount, string reason, long? relatedPurchaseId = null);
        Result<PagedResult<BonusTransactionDto>> GetTransactionHistory(long touristId, int page, int pageSize);
    }
}
