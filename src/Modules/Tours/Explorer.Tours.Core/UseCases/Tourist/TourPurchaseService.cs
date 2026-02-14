using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Stakeholders.Core.Domain;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.API.Public.Internal;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist
{
    public class TourPurchaseService : ITourPurchaseService
    {
        private readonly ICrudRepository<TourPurchase> _purchaseRepository;
        private readonly ICrudRepository<Tour> _tourRepository;
        private readonly ICrudRepository<Person> _personRepository;
        private readonly IShoppingCartService _cartService;
        private readonly IBonusPointsService _bonusPointsService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;

        public TourPurchaseService(
            ICrudRepository<TourPurchase> purchaseRepository,
            ICrudRepository<Tour> tourRepository,
            ICrudRepository<Person> personRepository,
            IShoppingCartService cartService,
            IBonusPointsService bonusPointsService,
            IEmailService emailService,
            IMapper mapper)
        {
            _purchaseRepository = purchaseRepository;
            _tourRepository = tourRepository;
            _personRepository = personRepository;
            _cartService = cartService;
            _bonusPointsService = bonusPointsService;
            _emailService = emailService;
            _mapper = mapper;
        }

        public Result<TourPurchaseDto> ProcessPurchase(long touristId, decimal bonusPointsToUse)
        {
            try
            {
                // Get the cart
                var cartResult = _cartService.GetCart(touristId);
                if (cartResult.IsFailed)
                {
                    return Result.Fail("Failed to retrieve cart").WithErrors(cartResult.Errors);
                }

                var cart = cartResult.Value;
                if (cart.TourIds.Count == 0)
                {
                    return Result.Fail(FailureCode.InvalidArgument).WithError("Cart is empty");
                }

                // Get all tours and validate
                var tours = new List<Tour>();
                decimal totalAmount = 0;

                foreach (var tourId in cart.TourIds)
                {
                    var tour = _tourRepository.Get(tourId);
                    if (tour == null)
                    {
                        return Result.Fail(FailureCode.NotFound).WithError($"Tour with ID {tourId} not found");
                    }

                    if (tour.State != TourState.COMPLETE)
                    {
                        return Result.Fail(FailureCode.InvalidArgument).WithError($"Tour '{tour.Name}' is not published");
                    }

                    if (tour.Date <= DateTime.UtcNow)
                    {
                        return Result.Fail(FailureCode.InvalidArgument).WithError($"Tour '{tour.Name}' has already passed");
                    }

                    tours.Add(tour);
                    totalAmount += tour.Price;
                }

                // Validate bonus points
                if (bonusPointsToUse > 0)
                {
                    var bonusPointsResult = _bonusPointsService.GetBonusPoints(touristId);
                    if (bonusPointsResult.IsFailed)
                    {
                        return Result.Fail("Failed to validate bonus points").WithErrors(bonusPointsResult.Errors);
                    }

                    var availablePoints = bonusPointsResult.Value.AvailablePoints;
                    if (bonusPointsToUse > availablePoints)
                    {
                        return Result.Fail(FailureCode.InvalidArgument)
                            .WithError($"Insufficient bonus points. Available: {availablePoints}, Requested: {bonusPointsToUse}");
                    }

                    if (bonusPointsToUse > totalAmount)
                    {
                        return Result.Fail(FailureCode.InvalidArgument)
                            .WithError($"Bonus points cannot exceed total amount. Total: €{totalAmount}, Bonus: {bonusPointsToUse}");
                    }
                }

                // Create purchase
                var purchase = new TourPurchase(
                    touristId,
                    cart.TourIds,
                    totalAmount,
                    bonusPointsToUse
                );

                var createdPurchase = _purchaseRepository.Create(purchase);

                // Use bonus points if specified
                if (bonusPointsToUse > 0)
                {
                    var useBonusResult = _bonusPointsService.UseBonusPoints(touristId, bonusPointsToUse, "Tour Purchase");
                    if (useBonusResult.IsFailed)
                    {
                        return Result.Fail("Purchase created but failed to deduct bonus points").WithErrors(useBonusResult.Errors);
                    }
                }

                // Clear cart
                var clearCartResult = _cartService.ClearCart(touristId);
                if (clearCartResult.IsFailed)
                {
                    // Log warning but don't fail the purchase
                    Console.WriteLine($"Warning: Failed to clear cart for tourist {touristId}");
                }

                // CRITICAL: Get tourist email SYNCHRONOUSLY while DbContext is alive
                string? touristEmail = null;
                try
                {
                    var person = _personRepository.Get(touristId);
                    touristEmail = person?.Email;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not retrieve tourist email: {ex.Message}");
                }

                // Send purchase confirmation email (don't fail purchase if email fails)
                if (!string.IsNullOrEmpty(touristEmail))
                {
                    var emailCopy = touristEmail; // Capture for Task.Run closure

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var emailData = new PurchaseEmailData
                            {
                                PurchaseId = createdPurchase.Id,
                                TourNames = tours.Select(t => t.Name).ToList(),
                                TotalAmount = totalAmount,
                                BonusPointsUsed = bonusPointsToUse,
                                FinalAmount = createdPurchase.FinalAmount,
                                PurchaseDate = createdPurchase.PurchaseDate
                            };

                            await _emailService.SendPurchaseConfirmationWithEmailAsync(emailCopy, emailData);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to send purchase confirmation email: {ex.Message}");
                        }
                    });
                }

                Console.WriteLine($"Purchase completed for tourist {touristId}. Purchase ID: {createdPurchase.Id}");

                return Result.Ok(_mapper.Map<TourPurchaseDto>(createdPurchase));
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error processing purchase: {ex.Message}");
            }
        }

        public Result<PagedResult<TourPurchaseDto>> GetPurchaseHistory(long touristId, int page, int pageSize)
        {
            try
            {
                var purchases = _purchaseRepository.GetAll()
                    .Where(p => p.TouristId == touristId)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalCount = _purchaseRepository.GetAll()
                    .Count(p => p.TouristId == touristId);

                var purchaseDtos = purchases.Select(p => _mapper.Map<TourPurchaseDto>(p)).ToList();

                var result = new PagedResult<TourPurchaseDto>(purchaseDtos, totalCount);
                return Result.Ok(result);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving purchase history: {ex.Message}");
            }
        }

        public Result<TourPurchaseDto> GetPurchase(long purchaseId)
        {
            try
            {
                var purchase = _purchaseRepository.Get(purchaseId);
                if (purchase == null)
                {
                    return Result.Fail(FailureCode.NotFound).WithError("Purchase not found");
                }

                return Result.Ok(_mapper.Map<TourPurchaseDto>(purchase));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving purchase: {ex.Message}");
            }
        }
    }
}