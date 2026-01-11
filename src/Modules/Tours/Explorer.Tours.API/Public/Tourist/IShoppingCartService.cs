using Explorer.Tours.API.Dtos;
using FluentResults;

namespace Explorer.Tours.API.Public.Tourist
{
    public interface IShoppingCartService
    {
        Result<ShoppingCartDto> GetCart(long touristId);
        Result<ShoppingCartDto> AddTourToCart(long touristId, long tourId);
        Result<ShoppingCartDto> RemoveTourFromCart(long touristId, long tourId);
        Result<ShoppingCartDto> ClearCart(long touristId);
    }
}
