using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist
{
    public class BonusPointsService : IBonusPointsService
    {
        private readonly ICrudRepository<BonusPoints> _bonusPointsRepository;
        private readonly ICrudRepository<BonusTransaction> _transactionRepository;
        private readonly IMapper _mapper;

        public BonusPointsService(ICrudRepository<BonusPoints> bonusPointsRepository, ICrudRepository<BonusTransaction> transactionRepository, IMapper mapper)
        {
            _bonusPointsRepository = bonusPointsRepository;
            _transactionRepository = transactionRepository;
            _mapper = mapper;
        }

        public Result<BonusPointsDto> GetBonusPoints(long touristId)
        {
            try
            {
                var bonusPoints = GetOrCreateBonusPoints(touristId);
                return Result.Ok(_mapper.Map<BonusPointsDto>(bonusPoints));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving bonus points: {ex.Message}");
            }
        }

        public Result<BonusPointsDto> AddBonusPoints(long touristId, decimal amount, string reason, long? relatedTourId = null)
        {
            try
            {
                var bonusPoints = GetOrCreateBonusPoints(touristId);
                bonusPoints.AddPoints(amount, reason);

                var updatedBonusPoints = _bonusPointsRepository.Update(bonusPoints);

                // Create transaction record
                var transaction = new BonusTransaction(
                    touristId,
                    amount, // Positive for earned
                    Domain.BonusTransactionType.EARNED_FROM_CANCELLATION,
                    reason,
                    relatedTourId);

                _transactionRepository.Create(transaction);

                return Result.Ok(_mapper.Map<BonusPointsDto>(updatedBonusPoints));
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error adding bonus points: {ex.Message}");
            }
        }

        public Result<BonusPointsDto> UseBonusPoints(long touristId, decimal amount, string reason, long? relatedPurchaseId = null)
        {
            try
            {
                Console.WriteLine($"UseBonusPoints called: touristId={touristId}, amount={amount}, reason={reason}, relatedPurchaseId={relatedPurchaseId}");

                var bonusPoints = GetOrCreateBonusPoints(touristId);

                if (!bonusPoints.HasSufficientPoints(amount))
                {
                    return Result.Fail(FailureCode.InvalidArgument).WithError("Insufficient bonus points");
                }

                Console.WriteLine($"Before using points: {bonusPoints.AvailablePoints}");
                bonusPoints.UsePoints(amount, reason);
                Console.WriteLine($"After using points: {bonusPoints.AvailablePoints}");

                var updatedBonusPoints = _bonusPointsRepository.Update(bonusPoints);
                Console.WriteLine($"Updated bonus points in DB: {updatedBonusPoints.AvailablePoints}");

                // Create transaction record
                Console.WriteLine($"Creating transaction: touristId={touristId}, amount={-amount}");
                var transaction = new BonusTransaction(
                    touristId,
                    -amount, // Negative for spent
                    Domain.BonusTransactionType.SPENT_ON_PURCHASE,
                    reason,
                    null, // No related tour for spending
                    relatedPurchaseId);

                Console.WriteLine($"Transaction created in memory: {transaction.Amount}, {transaction.Type}, {transaction.Description}");

                var createdTransaction = _transactionRepository.Create(transaction);
                Console.WriteLine($"Transaction saved to DB: ID={createdTransaction.Id}");

                return Result.Ok(_mapper.Map<BonusPointsDto>(updatedBonusPoints));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UseBonusPoints: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Result.Fail($"Error using bonus points: {ex.Message}");
            }
        }

        public Result<PagedResult<BonusTransactionDto>> GetTransactionHistory(long touristId, int page, int pageSize)
        {
            try
            {
                var transactions = _transactionRepository.GetAll()
                    .Where(t => t.TouristId == touristId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalCount = _transactionRepository.GetAll()
                    .Count(t => t.TouristId == touristId);

                var transactionDtos = transactions.Select(t => _mapper.Map<BonusTransactionDto>(t)).ToList();

                var result = new PagedResult<BonusTransactionDto>(transactionDtos, totalCount);
                return Result.Ok(result);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving transaction history: {ex.Message}");
            }
        }

        private BonusPoints GetOrCreateBonusPoints(long touristId)
        {
            var existingBonusPoints = _bonusPointsRepository.GetAll()
                .FirstOrDefault(bp => bp.TouristId == touristId);

            if (existingBonusPoints != null)
            {
                return existingBonusPoints;
            }

            // Create new bonus points record if doesn't exist
            var newBonusPoints = new BonusPoints(touristId, 0);
            return _bonusPointsRepository.Create(newBonusPoints);
        }
    }
}
