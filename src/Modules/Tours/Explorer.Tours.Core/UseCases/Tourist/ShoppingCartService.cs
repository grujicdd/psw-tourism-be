using AutoMapper;
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Tours.Core.Domain;
using FluentResults;

namespace Explorer.Tours.Core.UseCases.Tourist
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ICrudRepository<ShoppingCart> _cartRepository;
        private readonly ICrudRepository<Tour> _tourRepository;
        private readonly IMapper _mapper;

        public ShoppingCartService(ICrudRepository<ShoppingCart> cartRepository, ICrudRepository<Tour> tourRepository, IMapper mapper)
        {
            _cartRepository = cartRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public Result<ShoppingCartDto> GetCart(long touristId)
        {
            try
            {
                var cart = GetOrCreateCart(touristId);
                return Result.Ok(_mapper.Map<ShoppingCartDto>(cart));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error retrieving cart: {ex.Message}");
            }
        }

        public Result<ShoppingCartDto> AddTourToCart(long touristId, long tourId)
        {
            try
            {
                // Verify tour exists and is published
                var tour = _tourRepository.Get(tourId);
                if (tour == null)
                {
                    return Result.Fail(FailureCode.NotFound).WithError("Tour not found");
                }

                if (tour.State != TourState.COMPLETE)
                {
                    return Result.Fail(FailureCode.InvalidArgument).WithError("Tour is not published");
                }

                // Check if tour date is in the future
                if (tour.Date <= DateTime.UtcNow)
                {
                    return Result.Fail(FailureCode.InvalidArgument).WithError("Cannot add past tours to cart");
                }

                var cart = GetOrCreateCart(touristId);
                cart.AddTour(tourId);

                var updatedCart = _cartRepository.Update(cart);
                return Result.Ok(_mapper.Map<ShoppingCartDto>(updatedCart));
            }
            catch (ArgumentException ex)
            {
                return Result.Fail(FailureCode.InvalidArgument).WithError(ex.Message);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error adding tour to cart: {ex.Message}");
            }
        }

        public Result<ShoppingCartDto> RemoveTourFromCart(long touristId, long tourId)
        {
            try
            {
                var cart = GetOrCreateCart(touristId);
                cart.RemoveTour(tourId);

                var updatedCart = _cartRepository.Update(cart);
                return Result.Ok(_mapper.Map<ShoppingCartDto>(updatedCart));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error removing tour from cart: {ex.Message}");
            }
        }

        public Result<ShoppingCartDto> ClearCart(long touristId)
        {
            try
            {
                var cart = GetOrCreateCart(touristId);
                cart.ClearCart();

                var updatedCart = _cartRepository.Update(cart);
                return Result.Ok(_mapper.Map<ShoppingCartDto>(updatedCart));
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error clearing cart: {ex.Message}");
            }
        }

        private ShoppingCart GetOrCreateCart(long touristId)
        {
            var existingCart = _cartRepository.GetAll().FirstOrDefault(c => c.TouristId == touristId);

            if (existingCart != null)
            {
                return existingCart;
            }

            // Create new cart if doesn't exist
            var newCart = new ShoppingCart(touristId);
            return _cartRepository.Create(newCart);
        }
    }
}
