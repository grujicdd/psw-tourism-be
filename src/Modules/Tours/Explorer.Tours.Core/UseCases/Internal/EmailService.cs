// src/Modules/Tours/Explorer.Tours.Core/UseCases/Internal/EmailService.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.API.Public.Internal;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Configuration;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Explorer.Tours.Core.UseCases.Internal
{
    public class EmailService : IEmailService
    {
        private readonly ICrudRepository<Person> _personRepository;
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;
        private readonly IUserNotificationService _userNotificationService;

        public EmailService(
            ICrudRepository<Person> personRepository,
            ILogger<EmailService> logger,
            IOptions<EmailSettings> emailSettings,
            IUserNotificationService userNotificationService)
        {
            _personRepository = personRepository;
            _logger = logger;
            _emailSettings = emailSettings.Value;
            _userNotificationService = userNotificationService;
        }

        public async Task<Result> SendPurchaseConfirmationAsync(long touristId, PurchaseEmailData purchaseData)
        {
            try
            {
                var email = GetTouristEmail(touristId);
                if (string.IsNullOrEmpty(email))
                {
                    return Result.Fail("Could not retrieve tourist email");
                }

                var subject = "Purchase Confirmation - Your Tour Booking";
                var emailContent = GeneratePurchaseConfirmationEmail(email, purchaseData);

                return await SendEmailAsync(email, subject, emailContent);
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
                        var subject = $"Tour Cancellation - {cancellationData.TourName}";
                        var emailContent = GenerateCancellationEmail(email, cancellationData);
                        await SendEmailAsync(email, subject, emailContent);
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

                var subject = $"Tour Reminder - {reminderData.TourName} in 48 Hours!";
                var emailContent = GenerateReminderEmail(email, reminderData);

                return await SendEmailAsync(email, subject, emailContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tour reminder email to tourist {TouristId}", touristId);
                return Result.Fail($"Error sending email: {ex.Message}");
            }
        }

        private async Task<Result> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            // Always log the email
            _logger.LogInformation("========================================");
            _logger.LogInformation("EMAIL NOTIFICATION");
            _logger.LogInformation("To: {Email}", recipientEmail);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Content:\n{Content}", body);
            _logger.LogInformation("========================================");

            // Check if real email sending is enabled
            if (!_emailSettings.EnableRealEmailSending)
            {
                _logger.LogInformation("Real email sending is DISABLED - email logged only");
                return Result.Ok();
            }

            // SAFETY CHECK: Only send to allowed test email
            if (recipientEmail != _emailSettings.AllowedTestEmail)
            {
                _logger.LogWarning(
                    "SAFETY: Skipping email to {Email} - only {AllowedEmail} is allowed for testing",
                    recipientEmail, _emailSettings.AllowedTestEmail);
                return Result.Ok();
            }

            // Send actual email via SMTP
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword),
                    Timeout = 10000 // 10 seconds timeout
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SmtpUsername, "Explorer Tours"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add(recipientEmail);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("✅ Email successfully sent to {Email}", recipientEmail);
                return Result.Ok();
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email}", recipientEmail);
                return Result.Fail($"Failed to send email: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {Email}", recipientEmail);
                return Result.Fail($"Failed to send email: {ex.Message}");
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

This is a friendly reminder that your tour is scheduled in 48 hours!

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

        public async Task<Result> SendTourRecommendationAsync(long tourId, TourRecommendationEmailData tourData)
        {
            try
            {
                // CRITICAL: Get emails IMMEDIATELY, SYNCHRONOUSLY
                // This ensures we query while DbContext is still alive
                var interestedEmails = _userNotificationService.GetInterestedUserEmails(tourData.TourCategory);

                if (!interestedEmails.Any())
                {
                    _logger.LogInformation("No users found interested in category {Category} for tour {TourId}",
                        tourData.TourCategory, tourId);
                    return Result.Ok();
                }

                _logger.LogInformation("Found {Count} interested users for tour {TourId} in category {Category}",
                    interestedEmails.Count, tourId, tourData.TourCategory);

                // NOW send emails in background with the email list we already retrieved
                _ = Task.Run(async () =>
                {
                    var emailsSent = 0;
                    foreach (var email in interestedEmails)
                    {
                        try
                        {
                            var subject = $"New Tour Recommendation - {tourData.TourName}";
                            var emailContent = GenerateTourRecommendationEmail(email, tourData);
                            await SendEmailAsync(email, subject, emailContent);
                            emailsSent++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send tour recommendation to {Email}", email);
                        }
                    }

                    _logger.LogInformation("Sent {Count} tour recommendation emails for tour {TourId}", emailsSent, tourId);
                });

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tour recommendations for tour {TourId}", tourId);
                return Result.Fail($"Error sending emails: {ex.Message}");
            }
        }

        public async Task<Result> SendPurchaseConfirmationWithEmailAsync(string email, PurchaseEmailData purchaseData)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Result.Fail("Email address is required");
                }

                var subject = "Purchase Confirmation - Your Tour Booking";
                var emailContent = GeneratePurchaseConfirmationEmail(email, purchaseData);

                return await SendEmailAsync(email, subject, emailContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending purchase confirmation email to {Email}", email);
                return Result.Fail($"Error sending email: {ex.Message}");
            }
        }

        private string GenerateTourRecommendationEmail(string email, TourRecommendationEmailData data)
        {
            return $@"
Dear Explorer,

Great news! A new tour matching your interests has just been published:

Tour Name: {data.TourName}
Description: {data.TourDescription}
Date: {data.TourDate:MMMM dd, yyyy 'at' HH:mm}
Price: €{data.TourPrice}

This tour was recommended based on your selected interests. Visit our platform to learn more and book this exciting experience!

You can manage your interests and notification preferences in your profile settings.

Best regards,
The Explorer Team
    ";
        }
    }
}