// src/Explorer.API/Controllers/Author/KeyPointsController.cs
using Explorer.Tours.API.Dtos;
using Explorer.Tours.API.Public.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Explorer.API.Controllers.Author
{
    [Authorize(Policy = "authorPolicy")]
    [Route("api/author/keypoints")]
    public class KeyPointsController : BaseApiController
    {
        private readonly IKeyPointService _keyPointService;

        public KeyPointsController(IKeyPointService keyPointService)
        {
            _keyPointService = keyPointService;
        }

        [HttpPost]
        public ActionResult<KeyPointDto> Create([FromBody] KeyPointDto keyPoint)
        {
            var result = _keyPointService.Create(keyPoint);
            return CreateResponse(result);
        }

        [HttpPut("{id:long}")]
        public ActionResult<KeyPointDto> Update(long id, [FromBody] KeyPointDto keyPoint)
        {
            keyPoint.Id = id;
            var result = _keyPointService.Update(keyPoint);
            return CreateResponse(result);
        }

        [HttpDelete("{id:long}")]
        public ActionResult Delete(long id)
        {
            var result = _keyPointService.Delete(id);
            return CreateResponse(result);
        }

        [HttpGet("{id:long}")]
        public ActionResult<KeyPointDto> Get(long id)
        {
            var result = _keyPointService.Get(id);
            return CreateResponse(result);
        }

        [HttpGet("tour/{tourId:long}")]
        public ActionResult<IEnumerable<KeyPointDto>> GetByTourId(long tourId)
        {
            var result = _keyPointService.GetByTourId(tourId);
            return CreateResponse(result);
        }
    }
}
