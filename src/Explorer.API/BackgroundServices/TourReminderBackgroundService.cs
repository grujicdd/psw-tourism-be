// src/Explorer.API/BackgroundServices/TourReminderBackgroundService.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Explorer.API.BackgroundServices
{
    public class TourReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TourReminderBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Ovu vrednost promeniti u zavisnosti da li se testira ili je prava aplikacija

        public TourReminderBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TourReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tour Reminder Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendReminders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking and sending tour reminders");
                }

                // Wait for the next check interval
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Tour Reminder Background Service stopped");
        }

        private async Task CheckAndSendReminders()
        {
            _logger.LogInformation("Checking for tours requiring reminders...");

            // Create a new scope to get scoped services
            using var scope = _serviceProvider.CreateScope();

            var purchaseRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<TourPurchase>>();
            var tourRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<Tour>>();
            var keyPointRepository = scope.ServiceProvider.GetRequiredService<ICrudRepository<KeyPoint>>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Calculate the time window: 48 hours from now (with a 1-hour buffer)
            var targetTime = DateTime.UtcNow.AddHours(48);
            var windowStart = targetTime.AddMinutes(-30); // 47.5 hours from now
            var windowEnd = targetTime.AddMinutes(30);     // 48.5 hours from now

            _logger.LogInformation("Checking for tours between {Start} and {End}",
                windowStart.ToString("yyyy-MM-dd HH:mm"),
                windowEnd.ToString("yyyy-MM-dd HH:mm"));

            // Get all tours in the 48-hour window
            var toursInWindow = tourRepository.GetAll()
                .Where(t => t.Date >= windowStart && t.Date <= windowEnd && t.State == TourState.COMPLETE)
                .ToList();

            if (toursInWindow.Count == 0)
            {
                _logger.LogInformation("No tours found in the 48-hour window");
                return;
            }

            _logger.LogInformation("Found {Count} tour(s) in the 48-hour window", toursInWindow.Count);

            // For each tour, find purchases that haven't received reminders
            foreach (var tour in toursInWindow)
            {
                var purchasesForTour = purchaseRepository.GetAll()
                    .Where(p => p.TourIds.Contains(tour.Id) &&
                                !p.ReminderSent &&
                                p.Status == PurchaseStatus.Completed)
                    .ToList();

                _logger.LogInformation("Tour '{TourName}' (ID: {TourId}) has {Count} purchase(s) needing reminders",
                    tour.Name, tour.Id, purchasesForTour.Count);

                foreach (var purchase in purchasesForTour)
                {
                    try
                    {
                        // Get key points for this tour
                        var keyPoints = keyPointRepository.GetAll()
                            .Where(kp => kp.TourId == tour.Id)
                            .OrderBy(kp => kp.Order)
                            .Select(kp => kp.Name)
                            .ToList();

                        // Prepare reminder data
                        var reminderData = new TourReminderEmailData
                        {
                            TourName = tour.Name,
                            TourDate = tour.Date,
                            TourDescription = tour.Description,
                            KeyPoints = keyPoints
                        };

                        // Send reminder email
                        var result = await emailService.SendTourReminderAsync(purchase.TouristId, reminderData);

                        if (result.IsSuccess)
                        {
                            // Mark reminder as sent
                            purchase.MarkReminderAsSent();
                            purchaseRepository.Update(purchase);

                            _logger.LogInformation(
                                "Successfully sent reminder for tour '{TourName}' to tourist {TouristId}",
                                tour.Name, purchase.TouristId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to send reminder for tour '{TourName}' to tourist {TouristId}: {Errors}",
                                tour.Name, purchase.TouristId, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error sending reminder for tour '{TourName}' to tourist {TouristId}",
                            tour.Name, purchase.TouristId);
                    }
                }
            }

            _logger.LogInformation("Finished checking and sending tour reminders");
        }
    }
}
