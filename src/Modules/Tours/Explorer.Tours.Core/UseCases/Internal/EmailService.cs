// src/Modules/Tours/Explorer.Tours.Core/UseCases/Internal/EmailService.cs
using Explorer.Tours.API.Public.Internal;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.Core.Domain;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Explorer.Tours.Core.UseCases.Internal
{
    public class EmailService : IEmailService
    {
        private readonly ICrudRepository<Person> _personRepository;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ICrudRepository<Person> personRepository, ILogger<EmailService> logger)
        {
            _personRepository = personRepository;
            _logger = logger;
        }

        public async Task<Result> SendPurchaseConfirmationAsync(long touristId, PurchaseEmailData purchaseData)
        {
            try
            {
                // Get tourist email directly from repository
                var email = GetTouristEmail(touristId);
                if (string.IsNullOrEmpty(email))
                {
                    return Result.Fail("Could not retrieve tourist email");
                }

                // For now, log the email instead of actually sending it
                var emailContent = GeneratePurchaseConfirmationEmail(email, purchaseData);

                _logger.LogInformation("PURCHASE CONFIRMATION EMAIL");
                _logger.LogInformation("To: {Email}", email);
                _logger.LogInformation("Subject: Purchase Confirmation - Your Tour Booking");
                _logger.LogInformation("Content: {Content}", emailContent);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending purchase confirmation email to tourist {TouristId}", touristId);
                return Result.Fail($"Error sending email: {ex.Message}");
            }
        }

        public async Task<Result> SendTourCancellationAsync(List<long> touristIds, TourCancellationEmailData cancellationData)
        {
            try
            {
                foreach (var touristId in touristIds)
                {
                    var email = GetTouristEmail(touristId);
                    if (!string.IsNullOrEmpty(email))
                    {
                        var emailContent = GenerateCancellationEmail(email, cancellationData);

                        _logger.LogInformation("TOUR CANCELLATION EMAIL");
                        _logger.LogInformation("To: {Email}", email);
                        _logger.LogInformation("Subject: Tour Cancellation - {TourName}", cancellationData.TourName);
                        _logger.LogInformation("Content: {Content}", emailContent);
                    }
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tour cancellation emails");
                return Result.Fail($"Error sending emails: {ex.Message}");
            }
        }

        public async Task<Result> SendTourReminderAsync(long touristId, TourReminderEmailData reminderData)
        {
            try
            {
                var email = GetTouristEmail(touristId);
                if (string.IsNullOrEmpty(email))
                {
                    return Result.Fail("Could not retrieve tourist email");
                }

                var emailContent = GenerateReminderEmail(email, reminderData);

                _logger.LogInformation("TOUR REMINDER EMAIL");
                _logger.LogInformation("To: {Email}", email);
                _logger.LogInformation("Subject: Tour Reminder - {TourName} Tomorrow", reminderData.TourName);
                _logger.LogInformation("Content: {Content}", emailContent);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tour reminder email to tourist {TouristId}", touristId);
                return Result.Fail($"Error sending email: {ex.Message}");
            }
        }

        private string? GetTouristEmail(long touristId)
        {
            try
            {
                var person = _personRepository.Get(touristId);
                return person?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email for tourist {TouristId}", touristId);
                return null;
            }
        }

        private string GeneratePurchaseConfirmationEmail(string email, PurchaseEmailData data)
        {
            return $@"
Dear Explorer Customer,

Thank you for your purchase! Your tour booking has been confirmed.

Purchase Details:
- Purchase ID: #{data.PurchaseId}
- Purchase Date: {data.PurchaseDate:MMMM dd, yyyy 'at' HH:mm}
- Tours Booked: {string.Join(", ", data.TourNames)}

Payment Summary:
- Subtotal: €{data.TotalAmount:F2}
{(data.BonusPointsUsed > 0 ? $"- Bonus Points Used: -€{data.BonusPointsUsed:F2}" : "")}
- Final Amount: €{data.FinalAmount:F2}

You will receive a tour reminder email 48 hours before each tour date.

Thank you for choosing Explorer!

Best regards,
The Explorer Team
            ";
        }

        private string GenerateCancellationEmail(string email, TourCancellationEmailData data)
        {
            return $@"
Dear Explorer Customer,

We regret to inform you that the following tour has been cancelled:

Tour: {data.TourName}
Original Date: {data.OriginalDate:MMMM dd, yyyy 'at' HH:mm}
Reason: {data.CancellationReason}

As compensation for this inconvenience, we have added €{data.RefundAmount:F2} in bonus points to your account. These points can be used for any future tour purchases.

We sincerely apologize for any inconvenience caused.

Best regards,
The Explorer Team
            ";
        }

        private string GenerateReminderEmail(string email, TourReminderEmailData data)
        {
            var keyPointsList = data.KeyPoints.Count > 0
                ? "\n\nTour Highlights:\n" + string.Join("\n", data.KeyPoints.Select(kp => $"• {kp}"))
                : "";

            return $@"
Dear Explorer Customer,

This is a friendly reminder that your tour is scheduled for tomorrow!

Tour Details:
- Tour: {data.TourName}
- Date: {data.TourDate:MMMM dd, yyyy 'at' HH:mm}
- Description: {data.TourDescription}
{keyPointsList}

Please be ready and on time for departure. We're excited to show you an amazing experience!

Best regards,
The Explorer Team
            ";
        }
    }
}