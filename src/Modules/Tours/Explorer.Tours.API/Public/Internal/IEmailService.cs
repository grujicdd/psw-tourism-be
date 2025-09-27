// src/Modules/Tours/Explorer.Tours.API/Public/Internal/IEmailService.cs
using FluentResults;

namespace Explorer.Tours.API.Public.Internal
{
    public interface IEmailService
    {
        Task<Result> SendPurchaseConfirmationAsync(long touristId, PurchaseEmailData purchaseData);
        Task<Result> SendTourCancellationAsync(List<long> touristIds, TourCancellationEmailData cancellationData);
        Task<Result> SendTourReminderAsync(long touristId, TourReminderEmailData reminderData);
    }

    public class PurchaseEmailData
    {
        public long PurchaseId { get; set; }
        public List<string> TourNames { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BonusPointsUsed { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime PurchaseDate { get; set; }

        public PurchaseEmailData()
        {
            TourNames = new List<string>();
        }
    }

    public class TourCancellationEmailData
    {
        public string TourName { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime OriginalDate { get; set; }
        public string CancellationReason { get; set; }
    }

    public class TourReminderEmailData
    {
        public string TourName { get; set; }
        public DateTime TourDate { get; set; }
        public string TourDescription { get; set; }
        public List<string> KeyPoints { get; set; }

        public TourReminderEmailData()
        {
            KeyPoints = new List<string>();
        }
    }
}