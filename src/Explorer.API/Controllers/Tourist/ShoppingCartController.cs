// src/Explorer.API/Controllers/Tourist/ShoppingCartController.cs
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Stakeholders.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist
{
    [Authorize(Policy = "touristPolicy")]
    [Route("api/tourist/cart")]
    public class ShoppingCartController : BaseApiController
    {
        private readonly IShoppingCartService _cartService;

        public ShoppingCartController(IShoppingCartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public ActionResult<ShoppingCartDto> GetCart()
        {
            var touristId = User.PersonId();
            var result = _cartService.GetCart(touristId);
            return CreateResponse(result);
        }

        [HttpPost("items/{tourId:long}")]
        public ActionResult<ShoppingCartDto> AddTourToCart(long tourId)
        {
            var touristId = User.PersonId();
            var result = _cartService.AddTourToCart(touristId, tourId);
            return CreateResponse(result);
        }

        [HttpDelete("items/{tourId:long}")]
        public ActionResult<ShoppingCartDto> RemoveTourFromCart(long tourId)
        {
            var touristId = User.PersonId();
            var result = _cartService.RemoveTourFromCart(touristId, tourId);
            return CreateResponse(result);
        }

        [HttpDelete]
        public ActionResult<ShoppingCartDto> ClearCart()
        {
            var touristId = User.PersonId();
            var result = _cartService.ClearCart(touristId);
            return CreateResponse(result);
        }
    }
}
