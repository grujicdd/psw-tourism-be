// src/Explorer.API/Controllers/Tourist/BonusPointsController.cs
using Explorer.BuildingBlocks.Core.UseCases;
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Tourist;
using Explorer.Stakeholders.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Tourist
{
    [Authorize(Policy = "touristPolicy")]
    [Route("api/tourist/bonus-points")]
    public class BonusPointsController : BaseApiController
    {
        private readonly IBonusPointsService _bonusPointsService;

        public BonusPointsController(IBonusPointsService bonusPointsService)
        {
            _bonusPointsService = bonusPointsService;
        }

        [HttpGet]
        public ActionResult<BonusPointsDto> GetBonusPoints()
        {
            var touristId = User.PersonId();
            var result = _bonusPointsService.GetBonusPoints(touristId);
            return CreateResponse(result);
        }

        [HttpGet("history")]
        public ActionResult<PagedResult<BonusTransactionDto>> GetTransactionHistory([FromQuery] int page = 0, [FromQuery] int pageSize = 10)
        {
            var touristId = User.PersonId();
            var result = _bonusPointsService.GetTransactionHistory(touristId, page, pageSize);
            return CreateResponse(result);
        }
    }
}
