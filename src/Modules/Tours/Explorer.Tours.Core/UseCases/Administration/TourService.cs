using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Administration;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Explorer.Tours.Core.UseCases.Administration
{
    public class TourService : CrudService<TourDto, Tour>, ITourService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<TourService> _logger;

        public TourService(
            ICrudRepository<Tour> repository,
            IMapper mapper,
            IEmailService emailService,
            ILogger<TourService> logger)
            : base(repository, mapper)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public override Result<TourDto> Create(TourDto tourDto)
        {
            try
            {
                var result = base.Create(tourDto);

                if (result.IsFailed)
                {
                    return result;
                }

                var createdTour = result.Value;

                if (createdTour.State == (int)TourState.COMPLETE)
                {
                    SendTourRecommendationsSync(createdTour);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tour");
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
        }

        public override Result<TourDto> Update(TourDto tourDto)
        {
            try
            {
                var result = base.Update(tourDto);

                if (result.IsFailed)
                {
                    return result;
                }

                var updatedTour = result.Value;

                if (updatedTour.State == (int)TourState.COMPLETE)
                {
                    SendTourRecommendationsSync(updatedTour);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tour");
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
        }

        private void SendTourRecommendationsSync(TourDto tour)
        {
            // CRITICAL: Call this SYNCHRONOUSLY so emails are retrieved 
            // BEFORE the HTTP request ends and DbContext is disposed
            try
            {
                var emailData = new TourRecommendationEmailData
                {
                    TourName = tour.Name,
                    TourDescription = tour.Description,
                    TourCategory = tour.Category,
                    TourDate = tour.Date,
                    TourPrice = tour.Price
                };

                // This will get emails synchronously, then send them in background
                _ = _emailService.SendTourRecommendationAsync(tour.Id, emailData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send tour recommendations for tour {TourId}", tour.Id);
            }
        }
    }
}