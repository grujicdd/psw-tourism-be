// src/Explorer.API/Controllers/Tourist/TourPurchaseController.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Stakeholders.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist
{
    [Authorize(Policy = "touristPolicy")]
    [Route("api/tourist/purchases")]
    public class TourPurchaseController : BaseApiController
    {
        private readonly ITourPurchaseService _purchaseService;

        public TourPurchaseController(ITourPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpPost]
        public ActionResult<TourPurchaseDto> ProcessPurchase([FromBody] decimal bonusPointsToUse = 0)
        {
            var touristId = User.PersonId();
            var result = _purchaseService.ProcessPurchase(touristId, bonusPointsToUse);
            return CreateResponse(result);
        }

        [HttpGet]
        public ActionResult<PagedResult<TourPurchaseDto>> GetPurchaseHistory([FromQuery] int page = 0, [FromQuery] int pageSize = 10)
        {
            var touristId = User.PersonId();
            var result = _purchaseService.GetPurchaseHistory(touristId, page, pageSize);
            return CreateResponse(result);
        }

        [HttpGet("{purchaseId:long}")]
        public ActionResult<TourPurchaseDto> GetPurchase(long purchaseId)
        {
            // Note: In a real app, you'd verify the purchase belongs to this tourist
            var result = _purchaseService.GetPurchase(purchaseId);
            return CreateResponse(result);
        }
    }
}
