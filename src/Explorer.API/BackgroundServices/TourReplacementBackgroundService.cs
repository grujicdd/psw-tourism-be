using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Explorer.API.BackgroundServices
{
    public class TourReplacementBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TourReplacementBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

        public TourReplacementBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TourReplacementBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tour Replacement Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndCancelExpiredReplacements();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking and cancelling expired tour replacements");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Tour Replacement Background Service stopped");
        }

        private async Task CheckAndCancelExpiredReplacements()
        {
            _logger.LogInformation("Checking for expired tour replacement requests...");

            using var scope = _serviceProvider.CreateScope();

            var replacementRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<TourReplacement>>();
            var tourRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<Tour>>();
            var purchaseRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<TourPurchase>>();
            var bonusPointsRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<BonusPoints>>();
            var bonusTransactionRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<BonusTransaction>>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Get cutoff time: 24 hours from now
            var cutoffTime = DateTime.UtcNow.AddHours(24);

            _logger.LogInformation("Looking for PENDING replacements for tours before {CutoffTime}",
                cutoffTime.ToString("yyyy-MM-dd HH:mm"));

            // Get all PENDING replacement requests
            var pendingReplacements = replacementRepository.GetAll()
                .Where(r => r.Status == TourReplacementStatus.PENDING)
                .ToList();

            _logger.LogInformation("Found {Count} total PENDING replacement requests", pendingReplacements.Count);

            // Filter for those where tour date is within 24 hours
            var expiredReplacements = new List<TourReplacement>();
            foreach (var replacement in pendingReplacements)
            {
                var tour = tourRepository.Get(replacement.TourId);
                if (tour != null && tour.Date <= cutoffTime)
                {
                    expiredReplacements.Add(replacement);
                }
            }

            _logger.LogInformation("Found {Count} expired replacement requests (tours within 24h)", expiredReplacements.Count);

            foreach (var replacement in expiredReplacements)
            {
                try
                {
                    await ProcessExpiredReplacement(
                        replacement,
                        replacementRepository,
                        tourRepository,
                        purchaseRepository,
                        bonusPointsRepository,
                        bonusTransactionRepository,
                        emailService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired replacement {ReplacementId}", replacement.Id);
                }
            }

            if (expiredReplacements.Count > 0)
            {
                _logger.LogInformation("Finished processing {Count} expired tour replacements", expiredReplacements.Count);
            }
        }

        private async Task ProcessExpiredReplacement(
            TourReplacement replacement,
            ICrudRepository<TourReplacement> replacementRepository,
            ICrudRepository<Tour> tourRepository,
            ICrudRepository<TourPurchase> purchaseRepository,
            ICrudRepository<BonusPoints> bonusPointsRepository,
            ICrudRepository<BonusTransaction> bonusTransactionRepository,
            IEmailService emailService)
        {
            // Mark replacement as EXPIRED
            replacement.MarkAsExpired();
            replacementRepository.Update(replacement);

            // Get the tour
            var tour = tourRepository.Get(replacement.TourId);
            if (tour == null)
            {
                _logger.LogWarning("Tour {TourId} not found for replacement {ReplacementId}",
                    replacement.TourId, replacement.Id);
                return;
            }

            _logger.LogInformation("Cancelling tour {TourId} ({TourName}) - no replacement found within 24h",
                tour.Id, tour.Name);

            // Get all purchases containing this tour
            var allPurchases = purchaseRepository.GetAll()
                .Where(p => p.Status == PurchaseStatus.Completed)
                .ToList();

            var purchases = allPurchases
                .Where(p => p.ContainsTour(tour.Id))
                .ToList();

            _logger.LogInformation("Found {Count} purchases to refund for tour {TourId}", purchases.Count, tour.Id);

            // Refund each tourist with bonus points
            foreach (var purchase in purchases)
            {
                try
                {
                    RefundTourist(
                        purchase.TouristId,
                        tour,
                        purchase,
                        bonusPointsRepository,
                        bonusTransactionRepository);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refunding tourist {TouristId} for tour {TourId}",
                        purchase.TouristId, tour.Id);
                }
            }

            // Send cancellation emails to all affected tourists
            var touristIds = purchases.Select(p => p.TouristId).Distinct().ToList();

            if (touristIds.Count > 0)
            {
                var cancellationData = new TourCancellationEmailData
                {
                    TourName = tour.Name,
                    OriginalDate = tour.Date,
                    CancellationReason = "The tour guide could not find a replacement guide.",
                    RefundAmount = tour.Price
                };

                await emailService.SendTourCancellationAsync(touristIds, cancellationData);

                _logger.LogInformation("Successfully cancelled tour {TourId} and notified {Count} tourists",
                    tour.Id, touristIds.Count);
            }
            else
            {
                _logger.LogInformation("Successfully cancelled tour {TourId} (no tourists to notify)", tour.Id);
            }
        }

        private void RefundTourist(
            long touristId,
            Tour tour,
            TourPurchase purchase,
            ICrudRepository<BonusPoints> bonusPointsRepository,
            ICrudRepository<BonusTransaction> bonusTransactionRepository)
        {
            // Get or create bonus points for tourist
            var bonusPoints = bonusPointsRepository.GetAll()
                .FirstOrDefault(bp => bp.TouristId == touristId);

            if (bonusPoints == null)
            {
                bonusPoints = new BonusPoints(touristId);
                bonusPoints = bonusPointsRepository.Create(bonusPoints);
            }

            // Add bonus points equal to tour price
            var refundAmount = tour.Price;
            bonusPoints.AddPoints(refundAmount, "Tour Cancellation Refund");
            bonusPointsRepository.Update(bonusPoints);

            // Create transaction record
            var transaction = new BonusTransaction(
                touristId,
                refundAmount,
                BonusTransactionType.EARNED_FROM_CANCELLATION,
                $"Refund for cancelled tour: {tour.Name}",
                tour.Id,
                purchase.Id
            );
            bonusTransactionRepository.Create(transaction);

            _logger.LogInformation("Refunded {Amount} bonus points to tourist {TouristId} for tour {TourId}",
                refundAmount, touristId, tour.Id);
        }
    }
}
